// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public static class MessageBuilderExtensions
{
    public static AbstractMessageBuilder SetTimestamp(this AbstractMessageBuilder builder, long timestamp)
    {
        builder.SetHeader(Messaging.MessageHeaders.TIMESTAMP, timestamp);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageId(this AbstractMessageBuilder builder, string messageId)
    {
        builder.SetHeader(Messaging.MessageHeaders.ID, messageId);
        return builder;
    }

    public static AbstractMessageBuilder SetUserId(this AbstractMessageBuilder builder, string userId)
    {
        builder.SetHeader(RabbitMessageHeaders.USER_ID, userId);
        return builder;
    }

    public static AbstractMessageBuilder SetAppId(this AbstractMessageBuilder builder, string appId)
    {
        builder.SetHeader(RabbitMessageHeaders.APP_ID, appId);
        return builder;
    }

    public static AbstractMessageBuilder SetClusterId(this AbstractMessageBuilder builder, string clusterId)
    {
        builder.SetHeader(RabbitMessageHeaders.CLUSTER_ID, clusterId);
        return builder;
    }

    public static AbstractMessageBuilder SetType(this AbstractMessageBuilder builder, string type)
    {
        builder.SetHeader(RabbitMessageHeaders.TYPE, type);
        return builder;
    }

    public static AbstractMessageBuilder SetCorrelationId(this AbstractMessageBuilder builder, string correlationId)
    {
        builder.SetHeader(RabbitMessageHeaders.CORRELATION_ID, correlationId);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyTo(this AbstractMessageBuilder builder, string replyTo)
    {
        builder.SetHeader(RabbitMessageHeaders.REPLY_TO, replyTo);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToAddress(this AbstractMessageBuilder builder, Address replyTo)
    {
        builder.SetHeader(RabbitMessageHeaders.REPLY_TO, replyTo.ToString());
        return builder;
    }

    public static AbstractMessageBuilder SetContentType(this AbstractMessageBuilder builder, string contentType)
    {
        builder.SetHeader(RabbitMessageHeaders.CONTENT_TYPE, contentType);
        return builder;
    }

    public static AbstractMessageBuilder SetContentEncoding(this AbstractMessageBuilder builder, string contentEncoding)
    {
        builder.SetHeader(RabbitMessageHeaders.CONTENT_ENCODING, contentEncoding);
        return builder;
    }

    public static AbstractMessageBuilder SetContentLength(this AbstractMessageBuilder builder, long contentLength)
    {
        builder.SetHeader(RabbitMessageHeaders.CONTENT_LENGTH, contentLength);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryMode(this AbstractMessageBuilder builder, MessageDeliveryMode deliveryMode)
    {
        builder.SetHeader(RabbitMessageHeaders.DELIVERY_MODE, deliveryMode);
        return builder;
    }

    public static AbstractMessageBuilder SetExpiration(this AbstractMessageBuilder builder, string expiration)
    {
        builder.SetHeader(RabbitMessageHeaders.EXPIRATION, expiration);
        return builder;
    }

    public static AbstractMessageBuilder SetPriority(this AbstractMessageBuilder builder, int priority)
    {
        builder.SetHeader(RabbitMessageHeaders.PRIORITY, priority);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedExchange(this AbstractMessageBuilder builder, string receivedExchange)
    {
        builder.SetHeader(RabbitMessageHeaders.RECEIVED_EXCHANGE, receivedExchange);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedRoutingKey(this AbstractMessageBuilder builder, string receivedRoutingKey)
    {
        builder.SetHeader(RabbitMessageHeaders.RECEIVED_ROUTING_KEY, receivedRoutingKey);
        return builder;
    }

    public static AbstractMessageBuilder SetRedelivered(this AbstractMessageBuilder builder, bool redelivered)
    {
        builder.SetHeader(RabbitMessageHeaders.REDELIVERED, redelivered);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryTag(this AbstractMessageBuilder builder, ulong deliveryTag)
    {
        builder.SetHeader(RabbitMessageHeaders.DELIVERY_TAG, deliveryTag);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageCount(this AbstractMessageBuilder builder, uint messageCount)
    {
        builder.SetHeader(RabbitMessageHeaders.MESSAGE_COUNT, messageCount);
        return builder;
    }

    public static AbstractMessageBuilder SetTimestampIfAbsent(this AbstractMessageBuilder builder, long timestamp)
    {
        builder.SetHeaderIfAbsent(Messaging.MessageHeaders.TIMESTAMP, timestamp);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageIdIfAbsent(this AbstractMessageBuilder builder, string messageId)
    {
        builder.SetHeaderIfAbsent(Messaging.MessageHeaders.ID, messageId);
        return builder;
    }

    public static AbstractMessageBuilder SetUserIdIfAbsent(this AbstractMessageBuilder builder, string userId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.USER_ID, userId);
        return builder;
    }

    public static AbstractMessageBuilder SetAppIdIfAbsent(this AbstractMessageBuilder builder, string appId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.APP_ID, appId);
        return builder;
    }

    public static AbstractMessageBuilder SetClusterIdIfAbsent(this AbstractMessageBuilder builder, string clusterId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CLUSTER_ID, clusterId);
        return builder;
    }

    public static AbstractMessageBuilder SetTypeIfAbsent(this AbstractMessageBuilder builder, string type)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.TYPE, type);
        return builder;
    }

    public static AbstractMessageBuilder SetCorrelationIdIfAbsent(this AbstractMessageBuilder builder, string correlationId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CORRELATION_ID, correlationId);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToIfAbsent(this AbstractMessageBuilder builder, string replyTo)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.REPLY_TO, replyTo);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToAddressIfAbsent(this AbstractMessageBuilder builder, Address replyTo)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.REPLY_TO, replyTo.ToString());
        return builder;
    }

    public static AbstractMessageBuilder SetContentTypeIfAbsent(this AbstractMessageBuilder builder, string contentType)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CONTENT_TYPE, contentType);
        return builder;
    }

    public static AbstractMessageBuilder SetContentEncodingIfAbsent(this AbstractMessageBuilder builder, string contentEncoding)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CONTENT_ENCODING, contentEncoding);
        return builder;
    }

    public static AbstractMessageBuilder SetContentLengthIfAbsent(this AbstractMessageBuilder builder, long contentLength)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CONTENT_LENGTH, contentLength);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryModeIfAbsent(this AbstractMessageBuilder builder, MessageDeliveryMode deliveryMode)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.DELIVERY_MODE, deliveryMode);
        return builder;
    }

    public static AbstractMessageBuilder SetExpirationIfAbsent(this AbstractMessageBuilder builder, string expiration)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.EXPIRATION, expiration);
        return builder;
    }

    public static AbstractMessageBuilder SetPriorityIfAbsent(this AbstractMessageBuilder builder, int priority)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.PRIORITY, priority);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedExchangeIfAbsent(this AbstractMessageBuilder builder, string receivedExchange)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.RECEIVED_EXCHANGE, receivedExchange);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedRoutingKeyIfAbsent(this AbstractMessageBuilder builder, string receivedRoutingKey)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.RECEIVED_ROUTING_KEY, receivedRoutingKey);
        return builder;
    }

    public static AbstractMessageBuilder SetRedeliveredIfAbsent(this AbstractMessageBuilder builder, bool redelivered)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.REDELIVERED, redelivered);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryTagIfAbsent(this AbstractMessageBuilder builder, ulong deliveryTag)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.DELIVERY_TAG, deliveryTag);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageCountIfAbsent(this AbstractMessageBuilder builder, uint messageCount)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.MESSAGE_COUNT, messageCount);
        return builder;
    }
}