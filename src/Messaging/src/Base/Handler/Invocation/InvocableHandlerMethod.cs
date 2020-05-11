// Copyright 2017 the original author or authors.
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

using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class InvocableHandlerMethod : HandlerMethod, IInvocableHandlerMethod
    {
        private static readonly object[] EMPTY_ARGS = Array.Empty<object>();

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
