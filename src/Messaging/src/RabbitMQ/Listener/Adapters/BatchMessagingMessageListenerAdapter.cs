// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

public class BatchMessagingMessageListenerAdapter : MessagingMessageListenerAdapter, IChannelAwareBatchMessageListener
{
    private IBatchingStrategy BatchingStrategy { get; }

    public BatchMessagingMessageListenerAdapter(IApplicationContext context, object bean, MethodInfo method, bool returnExceptions,
        IRabbitListenerErrorHandler errorHandler, IBatchingStrategy batchingStrategy, ILogger logger = null)
        : base(context, bean, method, returnExceptions, errorHandler, true, logger)
    {
        BatchingStrategy = batchingStrategy ?? new SimpleBatchingStrategy(0, 0, 0L);
    }

    public override void OnMessageBatch(IEnumerable<IMessage> messages, RC.IModel channel)
    {
        IMessage converted = null;

        if (IsMessageByteArrayList)
        {
            var list = new List<IMessage<byte[]>>();

            foreach (IMessage m in messages)
            {
                list.Add((IMessage<byte[]>)m);
            }

            converted = Message.Create(list);
        }
        else
        {
            if (IsMessageList)
            {
                IList messagingMessages = CreateMessageList(InferredArgumentType);

                foreach (IMessage message in messages)
                {
                    messagingMessages.Add(ToMessagingMessage(message));
                }

                converted = Message.Create(messagingMessages);
            }
            else
            {
                IList payloads = CreateList(InferredArgumentType);

                foreach (IMessage message in messages)
                {
                    PreProcessMessage(message);
                    object convertedObject = MessageConverter.FromMessage(message, InferredArgumentType);

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
        Type messageType = typeof(IMessage<>).MakeGenericType(type);
        Type listType = typeof(List<>).MakeGenericType(messageType);
        return (IList)Activator.CreateInstance(listType);
    }

    protected IList CreateList(Type type)
    {
        Type listType = typeof(List<>).MakeGenericType(type);
        return (IList)Activator.CreateInstance(listType);
    }

    protected IMessage ToMessagingMessage(IMessage amqpMessage)
    {
        if (BatchingStrategy.CanDebatch(amqpMessage.Headers))
        {
            var list = new List<object>();

            BatchingStrategy.DeBatch(amqpMessage, _ =>
            {
                object convertedObject = MessageConverter.FromMessage(amqpMessage, null);

                if (convertedObject == null)
                {
                    throw new MessageConversionException("Message converter returned null");
                }

                list.Add(convertedObject);
            });

            return RabbitMessageBuilder.WithPayload(list).CopyHeaders(amqpMessage.Headers).Build();
        }

        PreProcessMessage(amqpMessage);
        IMessageHeaders headers = amqpMessage.Headers;
        object convertedObject = MessageConverter.FromMessage(amqpMessage, InferredArgumentType);

        if (convertedObject == null)
        {
            throw new MessageConversionException("Message converter returned null");
        }

        AbstractMessageBuilder builder = convertedObject is IMessage message1
            ? RabbitMessageBuilder.FromMessage(message1)
            : RabbitMessageBuilder.WithPayload(convertedObject);

        IMessage message = builder.CopyHeadersIfAbsent(headers).Build();
        return message;
    }
}
