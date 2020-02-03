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

using Steeltoe.Common.Util;
using System;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.Handler
{
    public class HandlerMethod
    {
        protected readonly object bean;
        protected readonly MethodInfo method;
        protected readonly Type beanType;

        public object Bean
        {
            get { return bean; }
        }

        public MethodInfo Method
        {
            get { return method; }
        }

        public Type BeanType
        {
            get { return beanType; }
        }

        public HandlerMethod ResolvedFromHandlerMethod { get; }

        public HandlerMethod(object bean, MethodInfo method)
        {
            if (bean == null)
            {
                throw new ArgumentNullException(nameof(bean));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            this.bean = bean;
            beanType = bean.GetType();
            this.method = method;
        }

        public HandlerMethod(object bean, string methodName, params Type[] parameterTypes)
        {
            if (bean == null)
            {
                throw new ArgumentNullException(nameof(bean));
            }

            if (string.IsNullOrEmpty(nameof(methodName)))
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            this.bean = bean;
            beanType = bean.GetType();
            method = beanType.GetMethod(methodName, parameterTypes);
        }

        private HandlerMethod(HandlerMethod handlerMethod, object handler)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            bean = handler;
            beanType = handlerMethod.beanType;
            method = handlerMethod.method;
            ResolvedFromHandlerMethod = handlerMethod;
        }

        public virtual bool IsVoid
        {
            get
            {
                return method.ReturnType.Equals(typeof(void));
            }
        }

        public virtual string ShortLogMessage
        {
            get
            {
                var args = method.GetParameters().Length;
                return beanType.Name + "#" + method.Name + "[" + args + " args]";
            }
        }

        public virtual ParameterInfo[] MethodParameters
        {
            get { return method.GetParameters(); }
        }

        public virtual ParameterInfo ReturnType
        {
            get { return method.ReturnParameter; }
        }

        public virtual HandlerMethod CreateWithResolvedBean()
        {
            var handler = bean;
            return new HandlerMethod(this, handler);
        }

        protected static object FindProvidedArgument(ParameterInfo parameter, params object[] providedArgs)
        {
            if (!ObjectUtils.IsEmpty(providedArgs))
            {
                foreach (var providedArg in providedArgs)
                {
                    if (parameter.ParameterType.IsInstanceOfType(providedArg))
                    {
                        return providedArg;
                    }
                }
            }

            return null;
        }

        protected static string FormatArgumentError(ParameterInfo param, string message)
        {
            return "Could not resolve parameter [" + param.Position + "] in " +
                    param.Member.ToString() + (!string.IsNullOrEmpty(message) ? ": " + message : string.Empty);
        }

        protected HandlerMethod(HandlerMethod handlerMethod)
        {
            if (handlerMethod == null)
            {
                throw new ArgumentNullException(nameof(handlerMethod));
            }

            bean = handlerMethod.bean;
            beanType = handlerMethod.beanType;
            method = handlerMethod.method;
        }

        protected virtual void AssertTargetBean(MethodInfo method, object targetBean, object[] args)
        {
            var methodDeclaringClass = method.DeclaringType;
            var targetBeanClass = targetBean.GetType();
            if (!methodDeclaringClass.IsAssignableFrom(targetBeanClass))
            {
                var text = "The mapped handler method class '" + methodDeclaringClass.Name +
                        "' is not an instance of the actual endpoint bean class '" +
                        targetBeanClass.Name;
                throw new InvalidOperationException(FormatInvokeError(text, args));
            }
        }

        protected virtual string FormatInvokeError(string text, object[] args)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    sb.Append("[" + i + "] [type=" + args[i].GetType().FullName + "] [value=" + args[i] + "]");
                }
                else
                {
                    sb.Append("[" + i + "] [null]");
                }

                sb.Append("\n");
            }

            return text + "\n" +
                     "Endpoint [" + beanType.Name + "]\n" +
                     "Method [" + method.ToString() + "] " +
                     "with argument values:\n" + sb.ToString();
        }
    }
}
