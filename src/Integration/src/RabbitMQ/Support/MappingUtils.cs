// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;

namespace Steeltoe.Integration.RabbitMQ.Support;

public static class MappingUtils
{
    /// <summary>
    /// Map an o.s.m.Message to an o.s.a.core.Message. When using a <see cref="ContentTypeDelegatingMessageConverter" />, AmqpHeaders#CONTENT_TYPE and
    /// MessageHeaders#CONTENT_TYPE will be used for the selection, with the AMQP header taking precedence.
    /// </summary>
    /// <param name="requestMessage">
    /// The request message.
    /// </param>
    /// <param name="converter">
    /// The message converter to use.
    /// </param>
    /// <param name="headerMapper">
    /// The header mapper to use.
    /// </param>
    /// <param name="defaultDeliveryMode">
    /// The default delivery mode.
    /// </param>
    /// <param name="headersMappedLast">
    /// <c>true</c> if headers are mapped after conversion.
    /// </param>
    /// <returns>
    /// The mapped message.
    /// </returns>
    public static IMessage MapMessage(IMessage requestMessage, IMessageConverter converter, IRabbitHeaderMapper headerMapper,
        MessageDeliveryMode defaultDeliveryMode, bool headersMappedLast)
    {
        return DoMapMessage(requestMessage, converter, headerMapper, headersMappedLast, false);
    }

    private static IMessage DoMapMessage(IMessage message, IMessageConverter converter, IRabbitHeaderMapper headerMapper, bool headersMappedLast, bool reply)
    {
        var targetHeaders = new MessageHeaders();
        IMessage amqpMessage;

        if (!headersMappedLast)
        {
            MapHeaders(message.Headers, targetHeaders, headerMapper, reply);
        }

        amqpMessage = converter.ToMessage(message.Payload, targetHeaders);

        if (headersMappedLast)
        {
            MapHeaders(message.Headers, targetHeaders, headerMapper, reply);
        }

        return amqpMessage;
    }

    private static void MapHeaders(IMessageHeaders messageHeaders, IMessageHeaders targetHeaders, IRabbitHeaderMapper headerMapper, bool reply)
    {
        if (reply)
        {
            headerMapper.FromHeadersToReply(messageHeaders, targetHeaders);
        }
        else
        {
            headerMapper.FromHeadersToRequest(messageHeaders, targetHeaders);
        }
    }
}
