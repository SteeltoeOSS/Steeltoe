// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public static class MessageHeaderExtensions
{
    public static string AppId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.APP_ID);
    }

    public static string ClusterId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CLUSTER_ID);
    }

    public static string ConsumerQueue(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CONSUMER_QUEUE);
    }

    public static string ConsumerTag(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CONSUMER_TAG);
    }

    public static string ContentEncoding(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CONTENT_ENCODING);
    }

    public static long? ContentLength(this IMessageHeaders headers)
    {
        return headers.Get<long?>(RabbitMessageHeaders.CONTENT_LENGTH);
    }

    public static bool IsContentLengthSet(this IMessageHeaders headers)
    {
        var len = ContentLength(headers);
        if (len.HasValue)
        {
            return true;
        }

        return false;
    }

    public static string ContentType(this IMessageHeaders headers)
    {
        var contentType = headers.Get<object>(RabbitMessageHeaders.CONTENT_TYPE);
        return contentType?.ToString();
    }

    public static string CorrelationId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CORRELATION_ID);
    }

    public static int? Delay(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.X_DELAY);
    }

    public static MessageDeliveryMode? DeliveryMode(this IMessageHeaders headers)
    {
        return headers.Get<MessageDeliveryMode?>(RabbitMessageHeaders.DELIVERY_MODE);
    }

    public static ulong? DeliveryTag(this IMessageHeaders headers)
    {
        return headers.Get<ulong?>(RabbitMessageHeaders.DELIVERY_TAG);
    }

    public static bool IsDeliveryTagSet(this IMessageHeaders headers)
    {
        var result = DeliveryTag(headers);
        if (result.HasValue)
        {
            return true;
        }

        return false;
    }

    public static string Expiration(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.EXPIRATION);
    }

    public static Type InferredArgumentType(this IMessageHeaders headers)
    {
        return headers.Get<Type>(MessageHeaders.INFERRED_ARGUMENT_TYPE);
    }

    public static uint? MessageCount(this IMessageHeaders headers)
    {
        return headers.Get<uint?>(RabbitMessageHeaders.MESSAGE_COUNT);
    }

    public static string MessageId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.MESSAGE_ID);
    }

    public static int? Priority(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.PRIORITY);
    }

    public static ulong? PublishSequenceNumber(this IMessageHeaders headers)
    {
        return headers.Get<ulong?>(RabbitMessageHeaders.PUBLISH_SEQUENCE_NUMBER);
    }

    public static int? ReceivedDelay(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.RECEIVED_DELAY);
    }

    public static MessageDeliveryMode? ReceivedDeliveryMode(this IMessageHeaders headers)
    {
        return headers.Get<MessageDeliveryMode?>(RabbitMessageHeaders.RECEIVED_DELIVERY_MODE);
    }

    public static string ReceivedExchange(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.RECEIVED_EXCHANGE);
    }

    public static string ReceivedRoutingKey(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.RECEIVED_ROUTING_KEY);
    }

    public static string ReceivedUserId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.RECEIVED_USER_ID);
    }

    public static bool? Redelivered(this IMessageHeaders headers)
    {
        return headers.Get<bool?>(RabbitMessageHeaders.REDELIVERED);
    }

    public static string ReplyTo(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.REPLY_TO);
    }

    public static Address ReplyToAddress(this IMessageHeaders headers)
    {
        var results = headers.ReplyTo();
        if (results != null)
        {
            return new Address(results);
        }

        return null;
    }

    public static object Target(this IMessageHeaders headers)
    {
        return headers.Get<object>(RabbitMessageHeaders.TARGET);
    }

    public static MethodInfo TargetMethod(this IMessageHeaders headers)
    {
        return headers.Get<MethodInfo>(RabbitMessageHeaders.TARGET_METHOD);
    }

    public static long? Timestamp(this IMessageHeaders headers)
    {
        return headers.Get<long?>(MessageHeaders.TIMESTAMP);
    }

    public static string Type(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.TYPE);
    }

    public static string UserId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.USER_ID);
    }

    public static bool? FinalRetryForMessageWithNoId(this IMessageHeaders headers)
    {
        return headers.Get<bool?>(RabbitMessageHeaders.FINAL_RETRY_FOR_MESSAGE_WITH_NO_ID);
    }

    public static bool IsFinalRetryForMessageWithNoId(this IMessageHeaders headers)
    {
        var result = FinalRetryForMessageWithNoId(headers);
        if (result.HasValue)
        {
            return result.Value;
        }

        return false;
    }

    public static bool? LastInBatch(this IMessageHeaders headers)
    {
        return headers.Get<bool?>(RabbitMessageHeaders.LAST_IN_BATCH);
    }
}
