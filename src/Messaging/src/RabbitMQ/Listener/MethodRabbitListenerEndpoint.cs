// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class MethodRabbitListenerEndpoint : AbstractRabbitListenerEndpoint
{
    public MethodInfo Method { get; set; }

    public object Instance { get; set; }

    public IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

    public bool ReturnExceptions { get; set; }

    public IRabbitListenerErrorHandler ErrorHandler { get; set; }

    public MethodRabbitListenerEndpoint(IApplicationContext applicationContext, MethodInfo method, object instance, ILoggerFactory loggerFactory = null)
        : base(applicationContext, loggerFactory)
    {
        Method = method;
        Instance = instance;
    }

    protected override IMessageListener CreateMessageListener(IMessageListenerContainer container)
    {
        if (MessageHandlerMethodFactory == null)
        {
            throw new InvalidOperationException("Could not create message listener - MessageHandlerMethodFactory not set");
        }

        MessagingMessageListenerAdapter messageListener = CreateMessageListenerInstance();
        messageListener.HandlerAdapter = ConfigureListenerAdapter(messageListener);
        string replyToAddress = GetDefaultReplyToAddress();

        if (replyToAddress != null)
        {
            messageListener.SetResponseAddress(replyToAddress);
        }

        ISmartMessageConverter messageConverter = MessageConverter;

        if (messageConverter != null)
        {
            messageListener.MessageConverter = messageConverter;
        }

        if (ServiceResolver != null)
        {
            messageListener.SetServiceResolver(ServiceResolver);
        }

        return messageListener;
    }

    protected virtual HandlerAdapter ConfigureListenerAdapter(MessagingMessageListenerAdapter messageListener)
    {
        IInvocableHandlerMethod invocableHandlerMethod = MessageHandlerMethodFactory.CreateInvocableHandlerMethod(Instance, Method);
        return new HandlerAdapter(invocableHandlerMethod);
    }

    protected virtual MessagingMessageListenerAdapter CreateMessageListenerInstance()
    {
        if (BatchListener)
        {
            return new BatchMessagingMessageListenerAdapter(ApplicationContext, Instance, Method, ReturnExceptions, ErrorHandler, BatchingStrategy,
                LoggerFactory?.CreateLogger(typeof(BatchMessagingMessageListenerAdapter)));
        }

        return new MessagingMessageListenerAdapter(ApplicationContext, Instance, Method, ReturnExceptions, ErrorHandler,
            LoggerFactory?.CreateLogger(typeof(MessagingMessageListenerAdapter)));
    }

    protected override StringBuilder GetEndpointDescription()
    {
        return base.GetEndpointDescription().Append(" | bean='").Append(Instance).Append('\'').Append(" | method='").Append(Method).Append('\'');
    }

    private string GetDefaultReplyToAddress()
    {
        MethodInfo listenerMethod = Method;

        if (listenerMethod != null)
        {
            var ann = listenerMethod.GetCustomAttribute<SendToAttribute>();

            if (ann != null)
            {
                string[] destinations = ann.Destinations;

                if (destinations.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Invalid SendToAttribute on '{listenerMethod}' one destination must be set (got {string.Join(",", destinations)})");
                }

                return destinations.Length == 1 ? ResolveSendTo(destinations[0]) : string.Empty;
            }
        }

        return null;
    }

    private string ResolveSendTo(string value)
    {
        if (ApplicationContext != null)
        {
            string resolvedValue = ExpressionContext.ApplicationContext.ResolveEmbeddedValue(value);
            object result = Resolver.Evaluate(resolvedValue, ExpressionContext);

            if (result is string strResult)
            {
                return strResult;
            }
        }

        return value;
    }
}
