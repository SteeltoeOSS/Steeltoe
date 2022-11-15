// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Messaging.Handler.Invocation;

public class InvocableHandlerMethod : HandlerMethod, IInvocableHandlerMethod
{
    private static readonly object[] EmptyArgs = Array.Empty<object>();
    private readonly ILogger _logger;

    public HandlerMethodArgumentResolverComposite MessageMethodArgumentResolvers { get; set; } = new();

    public InvocableHandlerMethod(HandlerMethod handlerMethod, ILogger logger = null)
        : base(handlerMethod)
    {
        _logger = logger;
    }

    public InvocableHandlerMethod(object bean, MethodInfo method)
        : base(bean, method)
    {
    }

    public InvocableHandlerMethod(object bean, string methodName, params Type[] parameterTypes)
        : base(bean, methodName, parameterTypes)
    {
    }

    public virtual object Invoke(IMessage requestMessage, params object[] args)
    {
        object[] argValues = GetMethodArgumentValues(requestMessage, args);

        _logger?.LogTrace("Arguments: {arguments}", string.Join(", ", args));

        return DoInvoke(argValues);
    }

    protected virtual object[] GetMethodArgumentValues(IMessage message, params object[] providedArgs)
    {
        ParameterInfo[] parameters = MethodParameters;

        if (parameters.Length == 0)
        {
            return EmptyArgs;
        }

        object[] args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            args[i] = FindProvidedArgument(parameter, providedArgs);

            if (args[i] != null)
            {
                continue;
            }

            if (!MessageMethodArgumentResolvers.SupportsParameter(parameter))
            {
                throw new MethodArgumentResolutionException(message, parameter, FormatArgumentError(parameter, "No suitable resolver"));
            }

            try
            {
                args[i] = MessageMethodArgumentResolvers.ResolveArgument(parameter, message);
            }
            catch (Exception ex)
            {
                // Leave stack trace for later, exception may actually be resolved and handled..
                string error = ex.Message;

                if (error != null && !error.Contains(parameter.Name, StringComparison.Ordinal))
                {
                    _logger?.LogDebug(ex, $"Error resolving parameter: {parameter.Name}, error: {error}");
                }

                throw;
            }
        }

        return args;
    }

    protected virtual object DoInvoke(params object[] args)
    {
        try
        {
            if (InnerArgCount != args.Length)
            {
                throw new InvalidOperationException(FormatInvokeError("Argument count mismatch", args), new TargetParameterCountException());
            }

            object result = InnerInvoker(InnerHandler, args);

            if (result is Task resultAsTask)
            {
                bool isAsyncMethod = Method.CustomAttributes.Any(x => x.AttributeType.Name == nameof(AsyncStateMachineAttribute));

                if (isAsyncMethod)
                {
                    resultAsTask.Wait();
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is InvalidCastException)
        {
            AssertTargetBean(Method, Handler, args);
            string text = !string.IsNullOrEmpty(ex.Message) ? ex.Message : "Illegal argument";
            throw new InvalidOperationException(FormatInvokeError(text, args));
        }
        catch (Exception ex)
        {
            // Unwrap for HandlerExceptionResolvers ...
            Exception targetException = ex.GetBaseException();

            if (targetException != null)
            {
                throw targetException;
            }

            throw new InvalidOperationException(FormatInvokeError("Invocation failure", args), ex);
        }
    }
}
