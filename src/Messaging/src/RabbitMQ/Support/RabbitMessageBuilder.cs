// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Support;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public static class RabbitMessageBuilder
{
    public static AbstractMessageBuilder FromMessage<TPayload>(IMessage<TPayload> message)
    {
        return new RabbitMessageBuilder<TPayload>(message);
    }

    public static AbstractMessageBuilder FromMessage(IMessage message, Type payloadType = null)
    {
        var genParamType = MessageBuilder.GetGenericParamType(message, payloadType);
        var typeToCreate = typeof(RabbitMessageBuilder<>).MakeGenericType(genParamType);

        return (AbstractMessageBuilder)Activator.CreateInstance(
            typeToCreate,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
            null,
            new object[] { message },
            null,
            null);
    }

    public static AbstractMessageBuilder WithPayload<TPayload>(TPayload payload)
    {
        return new RabbitMessageBuilder<TPayload>(payload, new RabbitHeaderAccessor());
    }

    public static AbstractMessageBuilder WithPayload(object payload, Type payloadType = null)
    {
        var genParamType = MessageBuilder.GetGenericParamType(payload, payloadType);
        var typeToCreate = typeof(RabbitMessageBuilder<>).MakeGenericType(genParamType);

        return (AbstractMessageBuilder)Activator.CreateInstance(
            typeToCreate,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { payload, new RabbitHeaderAccessor() },
            null,
            null);
    }

    public static IMessage CreateMessage(object payload, IMessageHeaders messageHeaders, Type payloadType = null)
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (messageHeaders == null)
        {
            throw new ArgumentNullException(nameof(messageHeaders));
        }

        return Message.Create(payload, messageHeaders, payloadType);
    }
}

public class RabbitMessageBuilder<TPayload> : MessageBuilder<TPayload>
{
    protected internal RabbitMessageBuilder()
    {
    }

    protected internal RabbitMessageBuilder(IMessage<TPayload> message)
        : base(message)
    {
    }

    protected internal RabbitMessageBuilder(IMessage message)
        : base(message)
    {
    }

    protected internal RabbitMessageBuilder(RabbitHeaderAccessor accessor)
        : base(accessor)
    {
    }

    protected internal RabbitMessageBuilder(TPayload payload, RabbitHeaderAccessor accessor)
        : base(payload, accessor)
    {
    }
}
