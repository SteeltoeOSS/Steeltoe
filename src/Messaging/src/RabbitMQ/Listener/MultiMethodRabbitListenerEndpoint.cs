﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        public MultiMethodRabbitListenerEndpoint(IApplicationContext applicationContext, List<MethodInfo> methods, object instance, ILogger logger = null)
        : this(applicationContext, methods, null, instance, logger)
        {
        }

        public MultiMethodRabbitListenerEndpoint(IApplicationContext applicationContext, List<MethodInfo> methods, MethodInfo defaultMethod, object instance, ILogger logger = null)
            : base(applicationContext, defaultMethod, instance, logger)
        {
            Methods = methods;
        }

        public List<MethodInfo> Methods { get; }

        protected HandlerAdapter ConfigureListenerAdapter(IMessagingMessageListenerAdapter messageListener)
        {
            var invocableHandlerMethods = new List<IInvocableHandlerMethod>();
            IInvocableHandlerMethod defaultHandler = null;
            foreach (var method in Methods)
            {
                var handler = MessageHandlerMethodFactory.CreateInvocableHandlerMethod(Instance, method);
                invocableHandlerMethods.Add(handler);
                if (method.Equals(Method))
                {
                    defaultHandler = handler;
                }
            }

            return new HandlerAdapter(new DelegatingInvocableHandler(invocableHandlerMethods, defaultHandler, Instance, Resolver, ExpressionContext));
        }
    }
}
