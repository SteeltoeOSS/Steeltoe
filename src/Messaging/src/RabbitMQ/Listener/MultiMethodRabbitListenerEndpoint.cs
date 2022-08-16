// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class MultiMethodRabbitListenerEndpoint : MethodRabbitListenerEndpoint
{
    public IEnumerable<MethodInfo> Methods { get; }

    public MethodInfo DefaultMethod { get; }

    public MultiMethodRabbitListenerEndpoint(IApplicationContext applicationContext, IEnumerable<MethodInfo> methods, object instance,
        ILoggerFactory loggerFactory = null)
        : this(applicationContext, methods, null, instance, loggerFactory)
    {
    }

    public MultiMethodRabbitListenerEndpoint(IApplicationContext applicationContext, IEnumerable<MethodInfo> methods, MethodInfo defaultMethod, object instance,
        ILoggerFactory loggerFactory = null)
        : base(applicationContext, null, instance, loggerFactory)
    {
        Methods = methods;
        DefaultMethod = defaultMethod;
    }

    protected override HandlerAdapter ConfigureListenerAdapter(MessagingMessageListenerAdapter messageListener)
    {
        var invocableHandlerMethods = new List<IInvocableHandlerMethod>();
        IInvocableHandlerMethod defaultHandler = null;

        foreach (MethodInfo method in Methods)
        {
            IInvocableHandlerMethod handler = MessageHandlerMethodFactory.CreateInvocableHandlerMethod(Instance, method);
            invocableHandlerMethods.Add(handler);

            if (method.Equals(DefaultMethod))
            {
                defaultHandler = handler;
            }
        }

        return new HandlerAdapter(new DelegatingInvocableHandler(invocableHandlerMethods, defaultHandler, Instance, Resolver, ExpressionContext));
    }
}
