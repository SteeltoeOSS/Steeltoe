// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public class RabbitHeaderAccessor : MessageHeaderAccessor
{
    public const int IntMask = 32;
    public const int DefaultPriority = 0;

    public const string SpringBatchFormat = "springBatchFormat";
    public const string BatchFormatLengthHeader4 = "lengthHeader4";
    public const string SpringAutoDecompress = "springAutoDecompress";
    public const string XDelay = "x-delay";
    public const string DefaultContentType = Messaging.MessageHeaders.ContentTypeBytes;

    public const MessageDeliveryMode DefaultDeliveryMode = MessageDeliveryMode.Persistent;

    public string AppId
    {
        get => GetHeader(RabbitMessageHeaders.AppId) as string;
        set => SetHeader(RabbitMessageHeaders.AppId, value);
    }

    public string ClusterId
    {
        get => GetHeader(RabbitMessageHeaders.ClusterId) as string;
        set => SetHeader(RabbitMessageHeaders.ClusterId, value);
    }

    public string ConsumerQueue
    {
        get => GetHeader(RabbitMessageHeaders.ConsumerQueue) as string;
        set => SetHeader(RabbitMessageHeaders.ConsumerQueue, value);
    }

    public string ConsumerTag
    {
        get => GetHeader(RabbitMessageHeaders.ConsumerTag) as string;
        set => SetHeader(RabbitMessageHeaders.ConsumerTag, value);
    }

    public string ContentEncoding
    {
        get => GetHeader(RabbitMessageHeaders.ContentEncoding) as string;
        set => SetHeader(RabbitMessageHeaders.ContentEncoding, value);
    }

    public long? ContentLength
    {
        get => GetHeader(RabbitMessageHeaders.ContentLength) as long?;
        set => SetHeader(RabbitMessageHeaders.ContentLength, value);
    }

    public bool IsContentLengthSet => ContentLength.HasValue;

    public string CorrelationId
    {
        get => GetHeader(RabbitMessageHeaders.CorrelationId) as string;
        set => SetHeader(RabbitMessageHeaders.CorrelationId, value);
    }

    public int? Delay
    {
        get => GetHeader(XDelay) as int?;

        set
        {
            if (value == null || value.Value < 0)
            {
                SetHeader(XDelay, null);
            }
            else
            {
                SetHeader(XDelay, value);
            }
        }
    }

    public MessageDeliveryMode? DeliveryMode
    {
        get => GetHeader(RabbitMessageHeaders.DeliveryMode) as MessageDeliveryMode?;
        set => SetHeader(RabbitMessageHeaders.DeliveryMode, value);
    }

    public ulong? DeliveryTag
    {
        get => GetHeader(RabbitMessageHeaders.DeliveryTag) as ulong?;
        set => SetHeader(RabbitMessageHeaders.DeliveryTag, value);
    }

    public bool IsDeliveryTagSet => DeliveryTag.HasValue;

    public string Expiration
    {
        get => GetHeader(RabbitMessageHeaders.Expiration) as string;
        set => SetHeader(RabbitMessageHeaders.Expiration, value);
    }

    public Type InferredArgumentType
    {
        get => GetHeader(Messaging.MessageHeaders.InferredArgumentType) as Type;
        set => SetHeader(Messaging.MessageHeaders.InferredArgumentType, value);
    }

    public uint? MessageCount
    {
        get => GetHeader(RabbitMessageHeaders.MessageCount) as uint?;
        set => SetHeader(RabbitMessageHeaders.MessageCount, value);
    }

    public string MessageId
    {
        get => GetHeader(Messaging.MessageHeaders.IdName) as string;
        set => SetHeader(Messaging.MessageHeaders.IdName, value);
    }

    public int? Priority
    {
        get => GetHeader(RabbitMessageHeaders.Priority) as int?;
        set => SetHeader(RabbitMessageHeaders.Priority, value);
    }

    public ulong? PublishSequenceNumber
    {
        get => GetHeader(RabbitMessageHeaders.PublishSequenceNumber) as ulong?;
        set => SetHeader(RabbitMessageHeaders.PublishSequenceNumber, value);
    }

    public int? ReceivedDelay
    {
        get => GetHeader(RabbitMessageHeaders.ReceivedDelay) as int?;
        set => SetHeader(RabbitMessageHeaders.ReceivedDelay, value);
    }

    public MessageDeliveryMode? ReceivedDeliveryMode
    {
        get => GetHeader(RabbitMessageHeaders.ReceivedDeliveryMode) as MessageDeliveryMode?;
        set => SetHeader(RabbitMessageHeaders.ReceivedDeliveryMode, value);
    }

    public string ReceivedExchange
    {
        get => GetHeader(RabbitMessageHeaders.ReceivedExchange) as string;
        set => SetHeader(RabbitMessageHeaders.ReceivedExchange, value);
    }

    public string ReceivedRoutingKey
    {
        get => GetHeader(RabbitMessageHeaders.ReceivedRoutingKey) as string;
        set => SetHeader(RabbitMessageHeaders.ReceivedRoutingKey, value);
    }

    public string ReceivedUserId
    {
        get => GetHeader(RabbitMessageHeaders.ReceivedUserId) as string;
        set => SetHeader(RabbitMessageHeaders.ReceivedUserId, value);
    }

    public bool? Redelivered
    {
        get => GetHeader(RabbitMessageHeaders.Redelivered) as bool?;
        set => SetHeader(RabbitMessageHeaders.Redelivered, value);
    }

    public string ReplyTo
    {
        get => GetHeader(RabbitMessageHeaders.ReplyTo) as string;
        set => SetHeader(RabbitMessageHeaders.ReplyTo, value);
    }

    public Address ReplyToAddress
    {
        get
        {
            string result = ReplyTo;

            if (result != null)
            {
                return new Address(result);
            }

            return null;
        }

        set => ReplyTo = value.ToString();
    }

    public object Target
    {
        get => GetHeader(RabbitMessageHeaders.Target);
        set => SetHeader(RabbitMessageHeaders.Target, value);
    }

    public MethodInfo TargetMethod
    {
        get => GetHeader(RabbitMessageHeaders.TargetMethod) as MethodInfo;
        set => SetHeader(RabbitMessageHeaders.TargetMethod, value);
    }

    public new long? Timestamp
    {
        get => base.Timestamp;
        set => SetHeader(Messaging.MessageHeaders.TimestampName, value);
    }

    public string Type
    {
        get => GetHeader(RabbitMessageHeaders.Type) as string;
        set => SetHeader(RabbitMessageHeaders.Type, value);
    }

    public string UserId
    {
        get => GetHeader(RabbitMessageHeaders.UserId) as string;
        set => SetHeader(RabbitMessageHeaders.UserId, value);
    }

    public bool? FinalRetryForMessageWithNoId
    {
        get => GetHeader(RabbitMessageHeaders.FinalRetryForMessageWithNoId) as bool?;
        set => SetHeader(RabbitMessageHeaders.FinalRetryForMessageWithNoId, value);
    }

    public bool IsFinalRetryForMessageWithNoId => FinalRetryForMessageWithNoId.HasValue;

    public bool? LastInBatch
    {
        get => GetHeader(RabbitMessageHeaders.LastInBatch) as bool?;
        set => SetHeader(RabbitMessageHeaders.LastInBatch, value);
    }

    public RabbitHeaderAccessor()
        : this((IMessage)null)
    {
    }

    public RabbitHeaderAccessor(IMessage message)
        : base(message)
    {
        headers = new RabbitAccessorMessageHeaders(this, message?.Headers);
    }

    protected internal RabbitHeaderAccessor(MessageHeaders headers)
        : base(headers)
    {
        this.headers = new RabbitAccessorMessageHeaders(this, headers);
    }

    public static RabbitHeaderAccessor GetAccessor(IMessage message)
    {
        return GetAccessor(message.Headers);
    }

    public static RabbitHeaderAccessor GetAccessor(IMessageHeaders messageHeaders)
    {
        if (messageHeaders is RabbitAccessorMessageHeaders accessorMessageHeaders)
        {
            return accessorMessageHeaders.Accessor;
        }

        if (messageHeaders is MessageHeaders msgHeaders)
        {
            return new RabbitHeaderAccessor(msgHeaders);
        }

        return null;
    }

    public static RabbitHeaderAccessor GetMutableAccessor(IMessage message)
    {
        return GetMutableAccessor(message.Headers);
    }

    public static RabbitHeaderAccessor GetMutableAccessor(IMessageHeaders headers)
    {
        RabbitHeaderAccessor messageHeaderAccessor = null;

        if (headers is RabbitAccessorMessageHeaders accessorMessageHeaders)
        {
            RabbitHeaderAccessor headerAccessor = accessorMessageHeaders.Accessor;
            messageHeaderAccessor = headerAccessor.IsMutable ? headerAccessor : headerAccessor.CreateMutableAccessor(headers);
        }

        if (messageHeaderAccessor == null && headers is MessageHeaders msgHeaders)
        {
            messageHeaderAccessor = new RabbitHeaderAccessor(msgHeaders);
        }

        return messageHeaderAccessor;
    }

    public List<Dictionary<string, object>> GetXDeathHeader()
    {
        return GetHeader(RabbitMessageHeaders.XDeath) as List<Dictionary<string, object>>;
    }

    public override IMessageHeaders ToMessageHeaders()
    {
        return Messaging.MessageHeaders.From(headers);
    }

    protected new RabbitHeaderAccessor CreateMutableAccessor(IMessage message)
    {
        return CreateMutableAccessor(message.Headers);
    }

    protected new RabbitHeaderAccessor CreateMutableAccessor(IMessageHeaders messageHeaders)
    {
        if (messageHeaders is not MessageHeaders headers)
        {
            throw new InvalidOperationException("Unable to create mutable accessor, message has no headers or headers are not of type MessageHeaders");
        }

        return new RabbitHeaderAccessor(headers);
    }

    protected override bool IsReadOnly(string headerName)
    {
        return !headers.IsMutable;
    }

    protected override void VerifyType(string headerName, object headerValue)
    {
        base.VerifyType(headerName, headerValue);

        if (RabbitMessageHeaders.Priority.Equals(headerName) && headerValue is not int)
        {
            throw new ArgumentException($"The '{headerName}' header value must be an {nameof(Int32)}.", nameof(headerName));
        }
    }

    protected class RabbitAccessorMessageHeaders : AccessorMessageHeaders
    {
        public new RabbitHeaderAccessor Accessor => accessor as RabbitHeaderAccessor;

        public RabbitAccessorMessageHeaders(MessageHeaderAccessor accessor, MessageHeaders headers)
            : base(accessor, headers)
        {
        }

        public RabbitAccessorMessageHeaders(MessageHeaderAccessor accessor, IDictionary<string, object> headers)
            : base(accessor, headers)
        {
        }
    }
}
