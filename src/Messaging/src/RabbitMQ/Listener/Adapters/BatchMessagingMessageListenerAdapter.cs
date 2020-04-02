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

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Data;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
{
    public class BatchMessagingMessageListenerAdapter : MessagingMessageListenerAdapter, IChannelAwareBatchMessageListener
    {
        public BatchMessagingMessageListenerAdapter(object bean, MethodInfo method, bool returnExceptions, IRabbitListenerErrorHandler errorHandler, IBatchingStrategy batchingStrategy)
        : base(bean, method, returnExceptions, errorHandler, true)
        {
            ConverterAdapter = (MessagingMessageConverterAdapter)MessagingMessageConverter;
            BatchingStrategy = batchingStrategy == null ? new SimpleBatchingStrategy(0, 0, 0L) : batchingStrategy;
        }

        private MessagingMessageConverterAdapter ConverterAdapter { get; }

        private IBatchingStrategy BatchingStrategy { get; }

        public override void OnMessageBatch(List<Message> messages, IModel channel)
        {
            IMessage converted;
            if (ConverterAdapter.IsAmqpMessageList)
            {
                converted = new GenericMessage(messages);
            }
            else
            {
                var messagingMessages = new List<IMessage>();
                foreach (var message in messages)
                {
                    messagingMessages.Add(ToMessagingMessage(message));
                }

                if (ConverterAdapter.IsMessageList)
                {
                    converted = new GenericMessage(messagingMessages);
                }
                else
                {
                    var payloads = new List<object>();
                    foreach (var message in messagingMessages)
                    {
                        payloads.Add(message.Payload);
                    }

                    converted = new GenericMessage(payloads);
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

        protected override IMessage ToMessagingMessage(Message amqpMessage)
        {
            if (BatchingStrategy.CanDebatch(amqpMessage.MessageProperties))
            {
                if (ConverterAdapter.IsMessageList)
                {
                    var messages = new List<IMessage>();
                    BatchingStrategy.DeBatch(amqpMessage, fragment =>
                    {
                        messages.Add(base.ToMessagingMessage(fragment));
                    });
                    return new GenericMessage(messages);
                }
                else
                {
                    var list = new List<object>();
                    BatchingStrategy.DeBatch(amqpMessage, fragment =>
                    {
                        list.Add(ConverterAdapter.ExtractPayload(fragment));
                    });
                    return Messaging.Support.MessageBuilder.WithPayload(list)
                            .CopyHeaders(ConverterAdapter.HeaderMapper.ToHeaders(amqpMessage.MessageProperties))
                            .Build();
                }
            }

            return base.ToMessagingMessage(amqpMessage);
        }
    }
}
