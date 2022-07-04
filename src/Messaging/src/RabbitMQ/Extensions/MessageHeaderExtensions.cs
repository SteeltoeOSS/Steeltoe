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
        return headers.Get<string>(RabbitMessageHeaders.AppId);
    }

    public static string ClusterId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ClusterId);
    }

    public static string ConsumerQueue(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ConsumerQueue);
    }

    public static string ConsumerTag(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ConsumerTag);
    }

    public static string ContentEncoding(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ContentEncoding);
    }

    public static long? ContentLength(this IMessageHeaders headers)
    {
        return headers.Get<long?>(RabbitMessageHeaders.ContentLength);
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
        var contentType = headers.Get<object>(RabbitMessageHeaders.ContentType);
        return contentType?.ToString();
    }

    public static string CorrelationId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.CorrelationId);
    }

    public static int? Delay(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.XDelay);
    }

    public static MessageDeliveryMode? DeliveryMode(this IMessageHeaders headers)
    {
        return headers.Get<MessageDeliveryMode?>(RabbitMessageHeaders.DeliveryMode);
    }

    public static ulong? DeliveryTag(this IMessageHeaders headers)
    {
        return headers.Get<ulong?>(RabbitMessageHeaders.DeliveryTag);
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
        return headers.Get<string>(RabbitMessageHeaders.Expiration);
    }

    public static Type InferredArgumentType(this IMessageHeaders headers)
    {
        return headers.Get<Type>(MessageHeaders.InferredArgumentType);
    }

    public static uint? MessageCount(this IMessageHeaders headers)
    {
        return headers.Get<uint?>(RabbitMessageHeaders.MessageCount);
    }

    public static string MessageId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.MessageId);
    }

    public static int? Priority(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.Priority);
    }

    public static ulong? PublishSequenceNumber(this IMessageHeaders headers)
    {
        return headers.Get<ulong?>(RabbitMessageHeaders.PublishSequenceNumber);
    }

    public static int? ReceivedDelay(this IMessageHeaders headers)
    {
        return headers.Get<int?>(RabbitMessageHeaders.ReceivedDelay);
    }

    public static MessageDeliveryMode? ReceivedDeliveryMode(this IMessageHeaders headers)
    {
        return headers.Get<MessageDeliveryMode?>(RabbitMessageHeaders.ReceivedDeliveryMode);
    }

    public static string ReceivedExchange(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ReceivedExchange);
    }

    public static string ReceivedRoutingKey(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ReceivedRoutingKey);
    }

    public static string ReceivedUserId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ReceivedUserId);
    }

    public static bool? Redelivered(this IMessageHeaders headers)
    {
        return headers.Get<bool?>(RabbitMessageHeaders.Redelivered);
    }

    public static string ReplyTo(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.ReplyTo);
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
        return headers.Get<object>(RabbitMessageHeaders.Target);
    }

    public static MethodInfo TargetMethod(this IMessageHeaders headers)
    {
        return headers.Get<MethodInfo>(RabbitMessageHeaders.TargetMethod);
    }

    public static long? Timestamp(this IMessageHeaders headers)
    {
        return headers.Get<long?>(MessageHeaders.TimestampName);
    }

    public static string Type(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.Type);
    }

    public static string UserId(this IMessageHeaders headers)
    {
        return headers.Get<string>(RabbitMessageHeaders.UserId);
    }

    public static bool? FinalRetryForMessageWithNoId(this IMessageHeaders headers)
    {
        return headers.Get<bool?>(RabbitMessageHeaders.FinalRetryForMessageWithNoId);
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
        return headers.Get<bool?>(RabbitMessageHeaders.LastInBatch);
    }
}
