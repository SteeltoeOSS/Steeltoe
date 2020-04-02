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
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class MethodRabbitListenerEndpoint : AbstractRabbitListenerEndpoint
    {
        public MethodRabbitListenerEndpoint(IApplicationContext applicationContext, MethodInfo method, object instance, ILogger logger = null)
            : base(applicationContext, logger)
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
                return new BatchMessagingMessageListenerAdapter(Instance, Method, ReturnExceptions, ErrorHandler, BatchingStrategy);
            }
            else
            {
                return new MessagingMessageListenerAdapter(Instance, Method, ReturnExceptions, ErrorHandler);
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
            // if (getBeanFactory() != null)
            // {
            //    String resolvedValue = getBeanExpressionContext().getBeanFactory().resolveEmbeddedValue(value);
            //    Object newValue = getResolver().evaluate(resolvedValue, getBeanExpressionContext());
            //    Assert.isInstanceOf(String, newValue, "Invalid @SendTo expression");
            //    return (String)newValue;
            // }
            // else
            // {
            //    return value;
            // }
            return value;
        }
    }
}
