// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using System;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    public class MethodRabbitListenerEndpoint : AbstractRabbitListenerEndpoint
    {
        public MethodRabbitListenerEndpoint(
            IApplicationContext applicationContext,
            MethodInfo method,
            object instance,
            ILoggerFactory loggerFactory = null)
            : base(applicationContext, loggerFactory)
        {
            Method = method;
            Instance = instance;
        }

        public MethodInfo Method { get; set; }

        public object Instance { get; set; }

        public IMessageHandlerMethodFactory MessageHandlerMethodFactory { get; set; }

        public bool ReturnExceptions { get; set; }

        public IRabbitListenerErrorHandler ErrorHandler { get; set; }

        protected override IMessageListener CreateMessageListener(IMessageListenerContainer container)
        {
            if (MessageHandlerMethodFactory == null)
            {
                throw new InvalidOperationException("Could not create message listener - MessageHandlerMethodFactory not set");
            }

            var messageListener = CreateMessageListenerInstance();
            messageListener.HandlerAdapter = ConfigureListenerAdapter(messageListener);
            var replyToAddress = GetDefaultReplyToAddress();
            if (replyToAddress != null)
            {
                messageListener.SetResponseAddress(replyToAddress);
            }

            var messageConverter = MessageConverter;
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
            var invocableHandlerMethod = MessageHandlerMethodFactory.CreateInvocableHandlerMethod(Instance, Method);
            return new HandlerAdapter(invocableHandlerMethod);
        }

        protected virtual MessagingMessageListenerAdapter CreateMessageListenerInstance()
        {
            if (BatchListener)
            {
                return new BatchMessagingMessageListenerAdapter(
                    ApplicationContext,
                    Instance,
                    Method,
                    ReturnExceptions,
                    ErrorHandler,
                    BatchingStrategy,
                    _loggerFactory?.CreateLogger(typeof(BatchMessagingMessageListenerAdapter)));
            }
            else
            {
                return new MessagingMessageListenerAdapter(
                    ApplicationContext,
                    Instance,
                    Method,
                    ReturnExceptions,
                    ErrorHandler,
                    _loggerFactory?.CreateLogger(typeof(MessagingMessageListenerAdapter)));
            }
        }

        protected override StringBuilder GetEndpointDescription()
        {
            return base.GetEndpointDescription()
                    .Append(" | bean='").Append(Instance).Append("'")
                    .Append(" | method='").Append(Method).Append("'");
        }

        private string GetDefaultReplyToAddress()
        {
            var listenerMethod = Method;
            if (listenerMethod != null)
            {
                var ann = listenerMethod.GetCustomAttribute<SendToAttribute>();
                if (ann != null)
                {
                    var destinations = ann.Destinations;
                    if (destinations.Length > 1)
                    {
                        throw new InvalidOperationException("Invalid SendToAttribute on '" + listenerMethod + "' one destination must be set (got " + string.Join(",", destinations) + ")");
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
                var resolvedValue = ExpressionContext.ApplicationContext.ResolveEmbeddedValue(value);
                var result = Resolver.Evaluate(resolvedValue, ExpressionContext);
                if (result is string)
                {
                    return (string)result;
                }
            }

        //value = PropertyPlaceholderHelper.ResolvePlaceholders(value, ApplicationContext.Configuration);
        //if (ConfigUtils.IsExpression(value))
        //{
        //    var serviceName = ConfigUtils.ExtractExpressionString(value);
        //    var queue = ApplicationContext.GetService<IQueue>(serviceName);
        //    if (queue != null)
        //    {
        //        value = queue.QueueName;
        //    }
        //}

            return value;
        }
    }
}
