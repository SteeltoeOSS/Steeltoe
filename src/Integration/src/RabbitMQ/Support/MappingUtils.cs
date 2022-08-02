// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Integration.RabbitMQ.Support;

public static class MappingUtils
{
    /*
     * Map an o.s.m.Message to an o.s.a.core.Message. When using a
     * {@link ContentTypeDelegatingMessageConverter}, {@link AmqpHeaders#CONTENT_TYPE} and
     * {@link MessageHeaders#CONTENT_TYPE} will be used for the selection, with the AMQP
     * header taking precedence.
     * @param requestMessage the request message.
     * @param converter the message converter to use.
     * @param headerMapper the header mapper to use.
     * @param defaultDeliveryMode the default delivery mode.
     * @param headersMappedLast true if headers are mapped after conversion.
     * @return the mapped Message.
     */
    public static IMessage MapMessage(IMessage requestMessage, IMessageConverter converter, IRabbitHeaderMapper headerMapper,
        MessageDeliveryMode defaultDeliveryMode, bool headersMappedLast)
    {
        return DoMapMessage(requestMessage, converter, headerMapper, defaultDeliveryMode, headersMappedLast, false);
    }

    private static IMessage DoMapMessage(IMessage message, IMessageConverter converter, IRabbitHeaderMapper headerMapper,
        MessageDeliveryMode defaultDeliveryMode, bool headersMappedLast, bool reply)
    {
        var targetHeaders = new MessageHeaders();
        IMessage amqpMessage;

        if (!headersMappedLast)
        {
            MapHeaders(message.Headers, targetHeaders, headerMapper, reply);
        }

        // if (converter is ContentTypeDe && headersMappedLast) {
        //    String contentType = contentTypeAsString(message.getHeaders());
        //    if (contentType != null)
        //    {
        //        amqpMessageProperties.setContentType(contentType);
        //    }
        // }
        amqpMessage = converter.ToMessage(message.Payload, targetHeaders);

        if (headersMappedLast)
        {
            MapHeaders(message.Headers, targetHeaders, headerMapper, reply);
        }

        // checkDeliveryMode(message, amqpMessageProperties, defaultDeliveryMode);
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

    // private static String contentTypeAsString(MessageHeaders headers)
    // {
    //    Object contentType = headers.get(AmqpHeaders.CONTENT_TYPE);
    //    if (contentType instanceof MimeType) {
    //        contentType = contentType.toString();
    //    }
    //    if (contentType instanceof String) {
    //        return (String)contentType;
    //    }

    // else if (contentType != null)
    //    {
    //        throw new IllegalArgumentException(AmqpHeaders.CONTENT_TYPE
    //                + " header must be a MimeType or String, found: " + contentType.getClass().getName());
    //    }
    //    return null;
    // }

    // /**
    // * Check the delivery mode and update with the default if not already present.
    // * @param requestMessage the request message.
    // * @param messageProperties the mapped message properties.
    // * @param defaultDeliveryMode the default delivery mode.
    // */
    // public static void checkDeliveryMode(Message<?> requestMessage, MessageProperties messageProperties,
    //        @Nullable MessageDeliveryMode defaultDeliveryMode)
    // {
    //    if (defaultDeliveryMode != null &&
    //            requestMessage.getHeaders().get(AmqpHeaders.DELIVERY_MODE) == null)
    //    {
    //        messageProperties.setDeliveryMode(defaultDeliveryMode);
    //    }
    // }
}
