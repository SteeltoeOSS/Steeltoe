// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Net.Sockets;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public abstract class AbstractMessageConverter : ISmartMessageConverter
{
    protected readonly ILogger _logger;

    protected AbstractMessageConverter(ILogger logger = null)
    {
        _logger = logger;
    }

    public bool CreateMessageIds { get; set; }

    public abstract string ServiceName { get; set; }

    public abstract object FromMessage(IMessage message, Type targetClass, object conversionHint);

    public T FromMessage<T>(IMessage message, object conversionHint)
    {
        return (T)FromMessage(message, typeof(T), conversionHint);
    }

    public object FromMessage(IMessage message, Type targetClass)
    {
        return FromMessage(message, targetClass, null);
    }

    public T FromMessage<T>(IMessage message)
    {
        return (T)FromMessage(message, typeof(T), null);
    }

    public IMessage ToMessage(object payload, IMessageHeaders messageProperties)
    {
        return ToMessage(payload, messageProperties, null);
    }

    public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
    {
        var messageProperties = headers;
        if (messageProperties == null)
        {
            messageProperties = new MessageHeaders();
        }

        var message = CreateMessage(payload, messageProperties, conversionHint);

        if (CreateMessageIds && message.Headers.MessageId() == null)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.MessageId = Guid.NewGuid().ToString();
        }

        return message;
    }

    protected abstract IMessage CreateMessage(object payload, IMessageHeaders messageProperties, object conversionHint);

    protected virtual IMessage CreateMessage(object payload, IMessageHeaders messageProperties)
    {
        return CreateMessage(payload, messageProperties, null);
    }
}