// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

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
        BatchingStrategy = batchingStrategy ?? new SimpleBatchingStrategy(0, 0, 0L);
    }

    private IBatchingStrategy BatchingStrategy { get; }

    public override void OnMessageBatch(List<IMessage> messages, RC.IModel channel)
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
                    PreProcessMessage(message);
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
            BatchingStrategy.DeBatch(amqpMessage, _ =>
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

        PreProcessMessage(amqpMessage);
        var headers = amqpMessage.Headers;
        var convertedObject = MessageConverter.FromMessage(amqpMessage, InferredArgumentType);
        if (convertedObject == null)
        {
            throw new MessageConversionException("Message converter returned null");
        }

        var builder = convertedObject is IMessage message1 ? RabbitMessageBuilder.FromMessage(message1) : RabbitMessageBuilder.WithPayload(convertedObject);
        var message = builder.CopyHeadersIfAbsent(headers).Build();
        return message;
    }
}
