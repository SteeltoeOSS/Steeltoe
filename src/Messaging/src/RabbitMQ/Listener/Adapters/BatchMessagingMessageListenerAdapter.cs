// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
