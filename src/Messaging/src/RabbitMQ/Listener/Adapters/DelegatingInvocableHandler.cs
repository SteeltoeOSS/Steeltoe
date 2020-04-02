﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Expression;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Expressions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class DelegatingInvocableHandler
    {
        private readonly Dictionary<IInvocableHandlerMethod, IExpression> _handlerSendTo = new Dictionary<IInvocableHandlerMethod, IExpression>();
        private readonly ConcurrentDictionary<Type, IInvocableHandlerMethod> _cachedHandlers = new ConcurrentDictionary<Type, IInvocableHandlerMethod>();

        public DelegatingInvocableHandler(List<IInvocableHandlerMethod> handlers, object bean, IServiceExpressionResolver resolver, IServiceExpressionContext context)
        : this(handlers, null, bean, resolver, context)
        {
        }

        public DelegatingInvocableHandler(List<IInvocableHandlerMethod> handlers, IInvocableHandlerMethod defaultHandler, object bean, IServiceExpressionResolver resolver, IServiceExpressionContext context)
        {
            Handlers = new List<IInvocableHandlerMethod>(handlers);
            DefaultHandler = defaultHandler;
            Bean = bean;
            Resolver = resolver;
            ServiceExpressionContext = context;
        }

        public List<IInvocableHandlerMethod> Handlers { get; }

        public IInvocableHandlerMethod DefaultHandler { get; }

        public object Bean { get; }

        public IServiceExpressionResolver Resolver { get; }

        public IServiceExpressionContext ServiceExpressionContext { get; }

        public bool HasDefaultHandler => DefaultHandler != null;

        public InvocationResult Invoke(IMessage message, params object[] providedArgs)
        {
            var payloadClass = message.Payload.GetType();
            var handler = GetHandlerForPayload(payloadClass);
            var result = handler.Invoke(message, providedArgs);
            if (!message.Headers.TryGetValue(AmqpHeaders.REPLY_TO, out _) && _handlerSendTo.TryGetValue(handler, out var replyTo))
            {
                return new InvocationResult(result, replyTo, handler.Method.ReturnType, handler.Bean, handler.Method);
            }

            return new InvocationResult(result, null, handler.Method.ReturnType, handler.Bean, handler.Method);
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
            var handler = FindHandlerForPayload(inboundPayload.GetType());
            if (handler != null)
            {
                _handlerSendTo.TryGetValue(handler, out var sendto);
                return new InvocationResult(result, sendto, handler.Method.ReturnType, handler.Bean, handler.Method);
            }

            return null;
        }

        protected IInvocableHandlerMethod GetHandlerForPayload(Type payloadClass)
        {
            if (!_cachedHandlers.TryGetValue(payloadClass, out var handler))
            {
                handler = FindHandlerForPayload(payloadClass);
                if (handler == null)
                {
                    throw new AmqpException("No method found for " + payloadClass);
                }

                _cachedHandlers.TryAdd(payloadClass, handler);
                SetupReplyTo(handler);
            }

            return handler;
        }

        protected virtual IInvocableHandlerMethod FindHandlerForPayload(Type payloadClass)
        {
            IInvocableHandlerMethod result = null;
            foreach (var handler in Handlers)
            {
                if (MatchHandlerMethod(payloadClass, handler))
                {
                    if (result != null)
                    {
                        var resultIsDefault = result.Equals(DefaultHandler);
                        if (!handler.Equals(DefaultHandler) && !resultIsDefault)
                        {
                            throw new AmqpException("Ambiguous methods for payload type: " + payloadClass + ": " + result.Method.Name + " and " + handler.Method.Name);
                        }

                        if (!resultIsDefault)
                        {
                            continue; // otherwise replace the result with the actual match
                        }
                    }

                    result = handler;
                }
            }

            return result != null ? result : DefaultHandler;
        }

        protected bool MatchHandlerMethod(Type payloadClass, IInvocableHandlerMethod handler)
        {
            var method = handler.Method;
            var parameters = method.GetParameters();
            var parameterAnnotations = GetParameterAnnotations(method);

            // Single param; no annotation or not @Header
            if (parameterAnnotations.Length == 1 && (parameterAnnotations[0].Length == 0 ||
                    !parameterAnnotations[0].Any((attr) => attr.GetType() == typeof(HeaderAttribute))) &&
                    parameters[0].ParameterType.IsAssignableFrom(payloadClass))
            {
                return true;
            }

            var foundCandidate = false;
            for (var i = 0; i < parameterAnnotations.Length; i++)
            {
                // MethodParameter methodParameter = new MethodParameter(method, i);
                if ((parameterAnnotations[i].Length == 0 ||
                        !parameterAnnotations[i].Any((attr) => attr.GetType() == typeof(HeaderAttribute))) &&
                        parameters[i].ParameterType.IsAssignableFrom(payloadClass))
                {
                    if (foundCandidate)
                    {
                        throw new AmqpException("Ambiguous payload parameter for " + method.ToString());
                    }

                    foundCandidate = true;
                }
            }

            return foundCandidate;
        }

        private Attribute[][] GetParameterAnnotations(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var attributes = new Attribute[parameters.Length][];
            var index = 0;
            foreach (var parameter in parameters)
            {
                attributes[index++] = parameter.GetCustomAttributes().ToArray();
            }

            return attributes;
        }

        private void SetupReplyTo(IInvocableHandlerMethod handler)
        {
            string replyTo = null;
            var method = handler.Method;
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
                throw new NotImplementedException("PARSER");

                // _handlerSendTo[handler] = PARSER.parseExpression(replyTo, PARSER_CONTEXT);
            }
        }

        private string ExtractSendTo(string element, SendToAttribute ann)
        {
            string replyTo = null;
            if (ann != null)
            {
                var destinations = ann.Destinations;
                if (destinations.Length > 1)
                {
                    throw new InvalidOperationException("Invalid SendToAttribute on '" + element + "' only one destination must be set");
                }

                replyTo = destinations.Length == 1 ? Resolve(destinations[0]) : null;
            }

            return replyTo;
        }

        private string Resolve(string value)
        {
            if (Resolver != null)
            {
                var resolvedValue = ServiceExpressionContext.ResolveEmbeddedValue(value);
                var newValue = Resolver.Evaluate(resolvedValue, ServiceExpressionContext);
                if (!(newValue is string))
                {
                    throw new InvalidOperationException("Invalid SendToAttribute expression");
                }

                return (string)newValue;
            }
            else
            {
                return value;
            }
        }
    }
}
