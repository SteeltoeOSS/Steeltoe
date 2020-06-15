// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class InvocableHandlerMethod : HandlerMethod, IInvocableHandlerMethod
    {
        private static readonly object[] EMPTY_ARGS = new object[0];

        public HandlerMethodArgumentResolverComposite MessageMethodArgumentResolvers { get; set; } = new HandlerMethodArgumentResolverComposite();

        public InvocableHandlerMethod(HandlerMethod handlerMethod)
        : base(handlerMethod)
        {
        }

        public InvocableHandlerMethod(object bean, MethodInfo method)
        : base(bean, method)
        {
        }

        public InvocableHandlerMethod(object bean, string methodName, params Type[] parameterTypes)
            : base(bean, methodName, parameterTypes)
        {
        }

        public virtual object Invoke(IMessage message, params object[] providedArgs)
        {
            var args = GetMethodArgumentValues(message, providedArgs);

            // if (logger.isTraceEnabled()) {
            // logger.trace("Arguments: " + Arrays.toString(args));
            // }
            return DoInvoke(args);
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

                // parameter.initParameterNameDiscovery(this.parameterNameDiscoverer);
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
                    // if (logger.isDebugEnabled())
                    // {
                    //    String error = ex.getMessage();
                    //    if (error != null && !error.contains(parameter.getExecutable().toGenericString()))
                    //    {
                    //        logger.debug(formatArgumentError(parameter, error));
                    //    }
                    // }
                    throw;
                }
            }

            return args;
        }

        protected virtual object DoInvoke(params object[] args)
        {
            try
            {
                return method.Invoke(bean, args);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is TargetParameterCountException)
            {
                AssertTargetBean(method, bean, args);
                var text = !string.IsNullOrEmpty(ex.Message) ? ex.Message : "Illegal argument";
                throw new InvalidOperationException(FormatInvokeError(text, args), ex);
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
