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
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class BatchMessagingMessageListenerAdapter : MessagingMessageListenerAdapter, IChannelAwareBatchMessageListener
    {
        public BatchMessagingMessageListenerAdapter(
            IApplicationContext context,
            object bean,
            MethodInfo method,
            bool returnExceptions,
            IRabbitListenerErrorHandler errorHandler,
            IBatchingStrategy batchingStrategy,
            ILogger logger = null)
            : base(context, bean, method, returnExceptions, errorHandler, true, logger)
        {
            BatchingStrategy = batchingStrategy == null ? new SimpleBatchingStrategy(0, 0, 0L) : batchingStrategy;
        }

        private IBatchingStrategy BatchingStrategy { get; }

        public override void OnMessageBatch(List<IMessage> messages, IModel channel)
        {
            IMessage converted = null;

            if (IsMessageByteArrayList)
            {
                var list = new List<IMessage<byte[]>>();
                foreach (var m in messages)
                {
                    list.Add((IMessage<byte[]>)m);
                }

                converted = Message.Create(list);
            }
            else
            {
                if (IsMessageList)
                {
                    var messagingMessages = CreateMessageList(InferredArgumentType);
                    foreach (var message in messages)
                    {
                        messagingMessages.Add(ToMessagingMessage(message));
                    }

                    converted = Message.Create(messagingMessages);
                }
                else
                {
                    var payloads = CreateList(InferredArgumentType);

                    foreach (var message in messages)
                    {
                        PreprocesMessage(message);
                        var convertedObject = MessageConverter.FromMessage(message, InferredArgumentType);
                        if (convertedObject == null)
                        {
                            throw new MessageConversionException("Message converter returned null");
                        }

                        payloads.Add(convertedObject);
                    }

                    converted = Message.Create(payloads);
                }
            }

            try
            {
                InvokeHandlerAndProcessResult(null, channel, converted);
            }
            catch (Exception e)
            {
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        protected IList CreateMessageList(Type type)
        {
            var messageType = typeof(IMessage<>).MakeGenericType(type);
            var listType = typeof(List<>).MakeGenericType(messageType);
            return (IList)Activator.CreateInstance(listType);
        }

        protected IList CreateList(Type type)
        {
            var listType = typeof(List<>).MakeGenericType(type);
            return (IList)Activator.CreateInstance(listType);
        }

        protected IMessage ToMessagingMessage(IMessage amqpMessage)
        {
            if (BatchingStrategy.CanDebatch(amqpMessage.Headers))
            {
                var list = new List<object>();
                BatchingStrategy.DeBatch(amqpMessage, fragment =>
                {
                    var convertedObject = MessageConverter.FromMessage(amqpMessage, null);
                    if (convertedObject == null)
                    {
                        throw new MessageConversionException("Message converter returned null");
                    }

                    list.Add(convertedObject);
                });

                return RabbitMessageBuilder.WithPayload(list).CopyHeaders(amqpMessage.Headers).Build();
            }

            PreprocesMessage(amqpMessage);
            var headers = amqpMessage.Headers;
            var convertedObject = MessageConverter.FromMessage(amqpMessage, InferredArgumentType);
            if (convertedObject == null)
            {
                throw new MessageConversionException("Message converter returned null");
            }

            var builder = (convertedObject is IMessage) ? RabbitMessageBuilder.FromMessage((IMessage)convertedObject) : RabbitMessageBuilder.WithPayload(convertedObject);
            var message = builder.CopyHeadersIfAbsent(headers).Build();
            return message;
        }
    }
}
