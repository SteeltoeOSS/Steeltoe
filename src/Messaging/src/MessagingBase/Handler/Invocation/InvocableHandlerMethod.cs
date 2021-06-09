﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class InvocableHandlerMethod : HandlerMethod, IInvocableHandlerMethod
    {
        private static readonly object[] EMPTY_ARGS = Array.Empty<object>();
        private readonly ILogger _logger;

        public HandlerMethodArgumentResolverComposite MessageMethodArgumentResolvers { get; set; } = new HandlerMethodArgumentResolverComposite();

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
            var argVals = GetMethodArgumentValues(requestMessage, args);

            _logger?.LogTrace("Arguments: " + string.Join(", ", args));

            return DoInvoke(argVals);
        }

        protected virtual object[] GetMethodArgumentValues(IMessage message, params object[] providedArgs)
        {
            var parameters = MethodParameters;

            if (parameters.Length == 0)
            {
                return EMPTY_ARGS;
            }

            var args = new object[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                args[i] = FindProvidedArgument(parameter, providedArgs);
                if (args[i] != null)
                {
                    continue;
                }

                if (!MessageMethodArgumentResolvers.SupportsParameter(parameter))
                {
                    throw new MethodArgumentResolutionException(
                            message, parameter, FormatArgumentError(parameter, "No suitable resolver"));
                }

                try
                {
                    args[i] = MessageMethodArgumentResolvers.ResolveArgument(parameter, message);
                }
                catch (Exception ex)
                {
                    // Leave stack trace for later, exception may actually be resolved and handled..
                    var error = ex.Message;
                    if (error != null && !error.Contains(parameter.Name))
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
                if (_argCount != args.Length)
                {
                    throw new InvalidOperationException(FormatInvokeError("Argument count mismatch", args), new TargetParameterCountException());
                }

                var result = _invoker(_handler, args);

                if (result is Task)
                {
                    var resultAsTask = result as Task;

                    var isAsyncMethod = Method.CustomAttributes
                        .Any(x => x.AttributeType.Name == nameof(System.Runtime.CompilerServices.AsyncStateMachineAttribute));

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
                var text = !string.IsNullOrEmpty(ex.Message) ? ex.Message : "Illegal argument";
                throw new InvalidOperationException(FormatInvokeError(text, args), new ArgumentException());
            }
            catch (Exception ex)
            {
                // Unwrap for HandlerExceptionResolvers ...
                var targetException = ex.GetBaseException();
                if (targetException != null)
                {
                    throw targetException;
                }
                else
                {
                    throw new InvalidOperationException(FormatInvokeError("Invocation failure", args), ex);
                }
            }
        }
    }
}
