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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class MultiMethodRabbitListenerEndpoint : MethodRabbitListenerEndpoint
    {
        public MultiMethodRabbitListenerEndpoint(
            IApplicationContext applicationContext,
            List<MethodInfo> methods,
            object instance,
            ILoggerFactory loggerFactory = null)
        : this(applicationContext, methods, null, instance, loggerFactory)
        {
        }

        public MultiMethodRabbitListenerEndpoint(
            IApplicationContext applicationContext,
            List<MethodInfo> methods,
            MethodInfo defaultMethod,
            object instance,
            ILoggerFactory loggerFactory = null)
            : base(applicationContext, null, instance, loggerFactory)
        {
            Methods = methods;
            DefaultMethod = defaultMethod;
        }

        public List<MethodInfo> Methods { get; }

        public MethodInfo DefaultMethod { get; }

        protected override HandlerAdapter ConfigureListenerAdapter(MessagingMessageListenerAdapter messageListener)
        {
            var invocableHandlerMethods = new List<IInvocableHandlerMethod>();
            IInvocableHandlerMethod defaultHandler = null;
            foreach (var method in Methods)
            {
                var handler = MessageHandlerMethodFactory.CreateInvocableHandlerMethod(Instance, method);
                invocableHandlerMethods.Add(handler);
                if (method.Equals(DefaultMethod))
                {
                    defaultHandler = handler;
                }
            }

            return new HandlerAdapter(new DelegatingInvocableHandler(invocableHandlerMethods, defaultHandler, Instance, Resolver, ExpressionContext));
        }
    }
}
