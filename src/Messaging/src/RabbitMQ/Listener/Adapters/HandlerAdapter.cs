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

using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class HandlerAdapter
    {
        public HandlerAdapter(IInvocableHandlerMethod invokerHandlerMethod)
        {
            InvokerHandlerMethod = invokerHandlerMethod;
            DelegatingHandler = null;
        }

        public HandlerAdapter(DelegatingInvocableHandler delegatingHandler)
        {
            InvokerHandlerMethod = null;
            DelegatingHandler = delegatingHandler;
        }

        public IInvocableHandlerMethod InvokerHandlerMethod { get; }

        public DelegatingInvocableHandler DelegatingHandler { get; }

        public InvocationResult Invoke(IMessage message, params object[] providedArgs)
        {
            if (InvokerHandlerMethod != null)
            {
                return new InvocationResult(
                    InvokerHandlerMethod.Invoke(message, providedArgs),
                    null,
                    InvokerHandlerMethod.Method.ReturnType,
                    InvokerHandlerMethod.Bean,
                    InvokerHandlerMethod.Method);
            }
            else if (DelegatingHandler.HasDefaultHandler)
            {
                // Needed to avoid returning raw Message which matches Object
                var args = new object[providedArgs.Length + 1];
                args[0] = message.Payload;
                Array.Copy(providedArgs, 0, args, 1, providedArgs.Length);
                return DelegatingHandler.Invoke(message, args);
            }
            else
            {
                return DelegatingHandler.Invoke(message, providedArgs);
            }
        }

        public string GetMethodAsString(object payload)
        {
            if (InvokerHandlerMethod != null)
            {
                return InvokerHandlerMethod.Method.ToString();
            }
            else
            {
                return DelegatingHandler.GetMethodNameFor(payload);
            }
        }

        public MethodInfo GetMethodFor(object payload)
        {
            if (InvokerHandlerMethod != null)
            {
                return InvokerHandlerMethod.Method;
            }
            else
            {
                return DelegatingHandler.GetMethodFor(payload);
            }
        }

        public Type GetReturnTypeFor(object payload)
        {
            if (InvokerHandlerMethod != null)
            {
                return InvokerHandlerMethod.Method.ReturnType;
            }
            else
            {
                return DelegatingHandler.GetMethodFor(payload).ReturnType;
            }
        }

        public object Instance
        {
            get
            {
                if (InvokerHandlerMethod != null)
                {
                    return InvokerHandlerMethod.Bean;
                }
                else
                {
                    return DelegatingHandler.Bean;
                }
            }
        }

        public InvocationResult GetInvocationResultFor(object result, object inboundPayload)
        {
            if (InvokerHandlerMethod != null)
            {
                return new InvocationResult(result, null, InvokerHandlerMethod.Method.ReturnType, InvokerHandlerMethod.Bean, InvokerHandlerMethod.Method);
            }
            else
            {
                return DelegatingHandler.GetInvocationResultFor(result, inboundPayload);
            }
        }
    }
}
