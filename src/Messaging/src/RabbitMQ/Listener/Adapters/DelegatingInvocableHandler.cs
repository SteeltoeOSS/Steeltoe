// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

public class DelegatingInvocableHandler
{
    private static readonly SpelExpressionParser Parser = new();
    private static readonly IParserContext ParserContext = new TemplateParserContext("!{", "}");

    private readonly Dictionary<IInvocableHandlerMethod, IExpression> _handlerSendTo = new();
    private readonly ConcurrentDictionary<Type, IInvocableHandlerMethod> _cachedHandlers = new();

    public List<IInvocableHandlerMethod> Handlers { get; }

    public IInvocableHandlerMethod DefaultHandler { get; }

    public object Bean { get; }

    public IServiceExpressionResolver Resolver { get; }

    public IServiceExpressionContext ServiceExpressionContext { get; }

    public bool HasDefaultHandler => DefaultHandler != null;

    public DelegatingInvocableHandler(List<IInvocableHandlerMethod> handlers, object bean, IServiceExpressionResolver resolver,
        IServiceExpressionContext context)
        : this(handlers, null, bean, resolver, context)
    {
    }

    public DelegatingInvocableHandler(List<IInvocableHandlerMethod> handlers, IInvocableHandlerMethod defaultHandler, object bean,
        IServiceExpressionResolver resolver, IServiceExpressionContext context)
    {
        Handlers = new List<IInvocableHandlerMethod>(handlers);
        DefaultHandler = defaultHandler;
        Bean = bean;
        Resolver = resolver;
        ServiceExpressionContext = context;
    }

    public InvocationResult Invoke(IMessage message, params object[] providedArgs)
    {
        Type payloadClass = message.Payload.GetType();
        IInvocableHandlerMethod handler = GetHandlerForPayload(payloadClass);
        object result = handler.Invoke(message, providedArgs);

        if (!message.Headers.TryGetValue(RabbitMessageHeaders.ReplyTo, out _) && _handlerSendTo.TryGetValue(handler, out IExpression replyTo))
        {
            return new InvocationResult(result, replyTo, handler.Method.ReturnType, handler.Handler, handler.Method);
        }

        return new InvocationResult(result, null, handler.Method.ReturnType, handler.Handler, handler.Method);
    }

    public string GetMethodNameFor(object payload)
    {
        IInvocableHandlerMethod handlerForPayload = null;

        try
        {
            handlerForPayload = GetHandlerForPayload(payload.GetType());
        }
        catch (Exception)
        {
            // Ignore
        }

        return handlerForPayload == null ? "no match" : handlerForPayload.Method.ToString();
    }

    public MethodInfo GetMethodFor(object payload)
    {
        return GetHandlerForPayload(payload.GetType()).Method;
    }

    public InvocationResult GetInvocationResultFor(object result, object inboundPayload)
    {
        IInvocableHandlerMethod handler = FindHandlerForPayload(inboundPayload.GetType());

        if (handler != null)
        {
            _handlerSendTo.TryGetValue(handler, out IExpression sendTo);
            return new InvocationResult(result, sendTo, handler.Method.ReturnType, handler.Handler, handler.Method);
        }

        return null;
    }

    protected IInvocableHandlerMethod GetHandlerForPayload(Type payloadClass)
    {
        if (!_cachedHandlers.TryGetValue(payloadClass, out IInvocableHandlerMethod handler))
        {
            handler = FindHandlerForPayload(payloadClass);

            if (handler == null)
            {
                throw new RabbitException($"No method found for {payloadClass}");
            }

            _cachedHandlers.TryAdd(payloadClass, handler);
            SetupReplyTo(handler);
        }

        return handler;
    }

    protected virtual IInvocableHandlerMethod FindHandlerForPayload(Type payloadClass)
    {
        IInvocableHandlerMethod result = null;

        foreach (IInvocableHandlerMethod handler in Handlers)
        {
            if (MatchHandlerMethod(payloadClass, handler))
            {
                if (result != null)
                {
                    bool resultIsDefault = result.Equals(DefaultHandler);

                    if (!handler.Equals(DefaultHandler) && !resultIsDefault)
                    {
                        throw new RabbitException($"Ambiguous methods for payload type: {payloadClass}: {result.Method.Name} and {handler.Method.Name}");
                    }

                    if (!resultIsDefault)
                    {
                        continue; // otherwise replace the result with the actual match
                    }
                }

                result = handler;
            }
        }

        return result ?? DefaultHandler;
    }

    protected bool MatchHandlerMethod(Type payloadClass, IInvocableHandlerMethod handler)
    {
        MethodInfo method = handler.Method;
        ParameterInfo[] parameters = method.GetParameters();
        Attribute[][] parameterAnnotations = GetParameterAnnotations(method);

        // Single param; no annotation or not @Header
        if (parameterAnnotations.Length == 1 && (parameterAnnotations[0].Length == 0 ||
            !parameterAnnotations[0].Any(attr => attr.GetType() == typeof(HeaderAttribute))) && parameters[0].ParameterType.IsAssignableFrom(payloadClass))
        {
            return true;
        }

        bool foundCandidate = false;

        for (int i = 0; i < parameterAnnotations.Length; i++)
        {
            // MethodParameter methodParameter = new MethodParameter(method, i);
            if ((parameterAnnotations[i].Length == 0 || !parameterAnnotations[i].Any(attr => attr.GetType() == typeof(HeaderAttribute))) &&
                parameters[i].ParameterType.IsAssignableFrom(payloadClass))
            {
                if (foundCandidate)
                {
                    throw new RabbitException($"Ambiguous payload parameter for {method}");
                }

                foundCandidate = true;
            }
        }

        return foundCandidate;
    }

    private Attribute[][] GetParameterAnnotations(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        var attributes = new Attribute[parameters.Length][];
        int index = 0;

        foreach (ParameterInfo parameter in parameters)
        {
            attributes[index++] = parameter.GetCustomAttributes().ToArray();
        }

        return attributes;
    }

    private void SetupReplyTo(IInvocableHandlerMethod handler)
    {
        string replyTo = null;
        MethodInfo method = handler.Method;

        if (method != null)
        {
            var ann = method.GetCustomAttribute<SendToAttribute>();
            replyTo = ExtractSendTo(method.ToString(), ann);
        }

        if (replyTo == null)
        {
            var ann = Bean.GetType().GetCustomAttribute<SendToAttribute>();
            replyTo = ExtractSendTo(Bean.GetType().Name, ann);
        }

        if (replyTo != null)
        {
            _handlerSendTo[handler] = Parser.ParseExpression(replyTo, ParserContext);
        }
    }

    private string ExtractSendTo(string element, SendToAttribute ann)
    {
        string replyTo = null;

        if (ann != null)
        {
            string[] destinations = ann.Destinations;

            if (destinations.Length > 1)
            {
                throw new InvalidOperationException($"Invalid SendToAttribute on '{element}' only one destination must be set");
            }

            replyTo = destinations.Length == 1 ? Resolve(destinations[0]) : null;
        }

        return replyTo;
    }

    private string Resolve(string value)
    {
        if (Resolver != null)
        {
            string resolvedValue = ServiceExpressionContext.ApplicationContext.ResolveEmbeddedValue(value);
            object newValue = Resolver.Evaluate(resolvedValue, ServiceExpressionContext);

            if (newValue is not string sValue)
            {
                throw new InvalidOperationException("Invalid SendToAttribute expression");
            }

            return sValue;
        }

        return value;
    }
}
