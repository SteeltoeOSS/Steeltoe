// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public static class MessageBuilderExtensions
{
    public static AbstractMessageBuilder SetTimestamp(this AbstractMessageBuilder builder, long timestamp)
    {
        builder.SetHeader(MessageHeaders.TimestampName, timestamp);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageId(this AbstractMessageBuilder builder, string messageId)
    {
        builder.SetHeader(MessageHeaders.IdName, messageId);
        return builder;
    }

    public static AbstractMessageBuilder SetUserId(this AbstractMessageBuilder builder, string userId)
    {
        builder.SetHeader(RabbitMessageHeaders.UserId, userId);
        return builder;
    }

    public static AbstractMessageBuilder SetAppId(this AbstractMessageBuilder builder, string appId)
    {
        builder.SetHeader(RabbitMessageHeaders.AppId, appId);
        return builder;
    }

    public static AbstractMessageBuilder SetClusterId(this AbstractMessageBuilder builder, string clusterId)
    {
        builder.SetHeader(RabbitMessageHeaders.ClusterId, clusterId);
        return builder;
    }

    public static AbstractMessageBuilder SetType(this AbstractMessageBuilder builder, string type)
    {
        builder.SetHeader(RabbitMessageHeaders.Type, type);
        return builder;
    }

    public static AbstractMessageBuilder SetCorrelationId(this AbstractMessageBuilder builder, string correlationId)
    {
        builder.SetHeader(RabbitMessageHeaders.CorrelationId, correlationId);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyTo(this AbstractMessageBuilder builder, string replyTo)
    {
        builder.SetHeader(RabbitMessageHeaders.ReplyTo, replyTo);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToAddress(this AbstractMessageBuilder builder, Address replyTo)
    {
        builder.SetHeader(RabbitMessageHeaders.ReplyTo, replyTo.ToString());
        return builder;
    }

    public static AbstractMessageBuilder SetContentType(this AbstractMessageBuilder builder, string contentType)
    {
        builder.SetHeader(RabbitMessageHeaders.ContentType, contentType);
        return builder;
    }

    public static AbstractMessageBuilder SetContentEncoding(this AbstractMessageBuilder builder, string contentEncoding)
    {
        builder.SetHeader(RabbitMessageHeaders.ContentEncoding, contentEncoding);
        return builder;
    }

    public static AbstractMessageBuilder SetContentLength(this AbstractMessageBuilder builder, long contentLength)
    {
        builder.SetHeader(RabbitMessageHeaders.ContentLength, contentLength);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryMode(this AbstractMessageBuilder builder, MessageDeliveryMode deliveryMode)
    {
        builder.SetHeader(RabbitMessageHeaders.DeliveryMode, deliveryMode);
        return builder;
    }

    public static AbstractMessageBuilder SetExpiration(this AbstractMessageBuilder builder, string expiration)
    {
        builder.SetHeader(RabbitMessageHeaders.Expiration, expiration);
        return builder;
    }

    public static AbstractMessageBuilder SetPriority(this AbstractMessageBuilder builder, int priority)
    {
        builder.SetHeader(RabbitMessageHeaders.Priority, priority);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedExchange(this AbstractMessageBuilder builder, string receivedExchange)
    {
        builder.SetHeader(RabbitMessageHeaders.ReceivedExchange, receivedExchange);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedRoutingKey(this AbstractMessageBuilder builder, string receivedRoutingKey)
    {
        builder.SetHeader(RabbitMessageHeaders.ReceivedRoutingKey, receivedRoutingKey);
        return builder;
    }

    public static AbstractMessageBuilder SetRedelivered(this AbstractMessageBuilder builder, bool redelivered)
    {
        builder.SetHeader(RabbitMessageHeaders.Redelivered, redelivered);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryTag(this AbstractMessageBuilder builder, ulong deliveryTag)
    {
        builder.SetHeader(RabbitMessageHeaders.DeliveryTag, deliveryTag);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageCount(this AbstractMessageBuilder builder, uint messageCount)
    {
        builder.SetHeader(RabbitMessageHeaders.MessageCount, messageCount);
        return builder;
    }

    public static AbstractMessageBuilder SetTimestampIfAbsent(this AbstractMessageBuilder builder, long timestamp)
    {
        builder.SetHeaderIfAbsent(MessageHeaders.TimestampName, timestamp);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageIdIfAbsent(this AbstractMessageBuilder builder, string messageId)
    {
        builder.SetHeaderIfAbsent(MessageHeaders.IdName, messageId);
        return builder;
    }

    public static AbstractMessageBuilder SetUserIdIfAbsent(this AbstractMessageBuilder builder, string userId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.UserId, userId);
        return builder;
    }

    public static AbstractMessageBuilder SetAppIdIfAbsent(this AbstractMessageBuilder builder, string appId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.AppId, appId);
        return builder;
    }

    public static AbstractMessageBuilder SetClusterIdIfAbsent(this AbstractMessageBuilder builder, string clusterId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ClusterId, clusterId);
        return builder;
    }

    public static AbstractMessageBuilder SetTypeIfAbsent(this AbstractMessageBuilder builder, string type)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.Type, type);
        return builder;
    }

    public static AbstractMessageBuilder SetCorrelationIdIfAbsent(this AbstractMessageBuilder builder, string correlationId)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.CorrelationId, correlationId);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToIfAbsent(this AbstractMessageBuilder builder, string replyTo)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ReplyTo, replyTo);
        return builder;
    }

    public static AbstractMessageBuilder SetReplyToAddressIfAbsent(this AbstractMessageBuilder builder, Address replyTo)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ReplyTo, replyTo.ToString());
        return builder;
    }

    public static AbstractMessageBuilder SetContentTypeIfAbsent(this AbstractMessageBuilder builder, string contentType)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ContentType, contentType);
        return builder;
    }

    public static AbstractMessageBuilder SetContentEncodingIfAbsent(this AbstractMessageBuilder builder, string contentEncoding)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ContentEncoding, contentEncoding);
        return builder;
    }

    public static AbstractMessageBuilder SetContentLengthIfAbsent(this AbstractMessageBuilder builder, long contentLength)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ContentLength, contentLength);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryModeIfAbsent(this AbstractMessageBuilder builder, MessageDeliveryMode deliveryMode)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.DeliveryMode, deliveryMode);
        return builder;
    }

    public static AbstractMessageBuilder SetExpirationIfAbsent(this AbstractMessageBuilder builder, string expiration)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.Expiration, expiration);
        return builder;
    }

    public static AbstractMessageBuilder SetPriorityIfAbsent(this AbstractMessageBuilder builder, int priority)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.Priority, priority);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedExchangeIfAbsent(this AbstractMessageBuilder builder, string receivedExchange)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ReceivedExchange, receivedExchange);
        return builder;
    }

    public static AbstractMessageBuilder SetReceivedRoutingKeyIfAbsent(this AbstractMessageBuilder builder, string receivedRoutingKey)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.ReceivedRoutingKey, receivedRoutingKey);
        return builder;
    }

    public static AbstractMessageBuilder SetRedeliveredIfAbsent(this AbstractMessageBuilder builder, bool redelivered)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.Redelivered, redelivered);
        return builder;
    }

    public static AbstractMessageBuilder SetDeliveryTagIfAbsent(this AbstractMessageBuilder builder, ulong deliveryTag)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.DeliveryTag, deliveryTag);
        return builder;
    }

    public static AbstractMessageBuilder SetMessageCountIfAbsent(this AbstractMessageBuilder builder, uint messageCount)
    {
        builder.SetHeaderIfAbsent(RabbitMessageHeaders.MessageCount, messageCount);
        return builder;
    }
}
