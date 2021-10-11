// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.RabbitMQ.Support
{
    public class RabbitHeaderAccessor : MessageHeaderAccessor
    {
        public const int INT_MASK = 32;
        public const int DEFAULT_PRIORITY = 0;

        public const string SPRING_BATCH_FORMAT = "springBatchFormat";
        public const string BATCH_FORMAT_LENGTH_HEADER4 = "lengthHeader4";
        public const string SPRING_AUTO_DECOMPRESS = "springAutoDecompress";
        public const string X_DELAY = "x-delay";
        public const string DEFAULT_CONTENT_TYPE = Steeltoe.Messaging.MessageHeaders.CONTENT_TYPE_BYTES;

        public const MessageDeliveryMode DEFAULT_DELIVERY_MODE = MessageDeliveryMode.PERSISTENT;

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
                var headerAccessor = accessorMessageHeaders.Accessor;
                messageHeaderAccessor = headerAccessor.IsMutable ? headerAccessor : headerAccessor.CreateMutableAccessor(headers);
            }

            if (messageHeaderAccessor == null && headers is MessageHeaders msgHeaders)
            {
                messageHeaderAccessor = new RabbitHeaderAccessor(msgHeaders);
            }

            return messageHeaderAccessor;
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
            headers = new RabbitAccessorMessageHeaders(this, headers);
        }

        public string AppId
        {
            get => GetHeader(RabbitMessageHeaders.APP_ID) as string;
            set => SetHeader(RabbitMessageHeaders.APP_ID, value);
        }

        public string ClusterId
        {
            get => GetHeader(RabbitMessageHeaders.CLUSTER_ID) as string;
            set => SetHeader(RabbitMessageHeaders.CLUSTER_ID, value);
        }

        public string ConsumerQueue
        {
            get => GetHeader(RabbitMessageHeaders.CONSUMER_QUEUE) as string;
            set => SetHeader(RabbitMessageHeaders.CONSUMER_QUEUE, value);
        }

        public string ConsumerTag
        {
            get => GetHeader(RabbitMessageHeaders.CONSUMER_TAG) as string;
            set => SetHeader(RabbitMessageHeaders.CONSUMER_TAG, value);
        }

        public string ContentEncoding
        {
            get => GetHeader(RabbitMessageHeaders.CONTENT_ENCODING) as string;
            set => SetHeader(RabbitMessageHeaders.CONTENT_ENCODING, value);
        }

        public long? ContentLength
        {
            get => GetHeader(RabbitMessageHeaders.CONTENT_LENGTH) as long?;
            set => SetHeader(RabbitMessageHeaders.CONTENT_LENGTH, value);
        }

        public bool IsContentLengthSet => ContentLength.HasValue;

        public string CorrelationId
        {
            get => GetHeader(RabbitMessageHeaders.CORRELATION_ID) as string;
            set => SetHeader(RabbitMessageHeaders.CORRELATION_ID, value);
        }

        public int? Delay
        {
            get => GetHeader(X_DELAY) as int?;

            set
            {
                if (value == null || value.Value < 0)
                {
                    SetHeader(X_DELAY, null);
                }
                else
                {
                    SetHeader(X_DELAY, value);
                }
            }
        }

        public MessageDeliveryMode? DeliveryMode
        {
            get => GetHeader(RabbitMessageHeaders.DELIVERY_MODE) as MessageDeliveryMode?;
            set => SetHeader(RabbitMessageHeaders.DELIVERY_MODE, value);
        }

        public ulong? DeliveryTag
        {
            get => GetHeader(RabbitMessageHeaders.DELIVERY_TAG) as ulong?;
            set => SetHeader(RabbitMessageHeaders.DELIVERY_TAG, value);
        }

        public bool IsDeliveryTagSet => DeliveryTag.HasValue;

        public string Expiration
        {
            get => GetHeader(RabbitMessageHeaders.EXPIRATION) as string;
            set => SetHeader(RabbitMessageHeaders.EXPIRATION, value);
        }

        public Type InferredArgumentType
        {
            get => GetHeader(Messaging.MessageHeaders.INFERRED_ARGUMENT_TYPE) as Type;
            set => SetHeader(Messaging.MessageHeaders.INFERRED_ARGUMENT_TYPE, value);
        }

        public uint? MessageCount
        {
            get => GetHeader(RabbitMessageHeaders.MESSAGE_COUNT) as uint?;
            set => SetHeader(RabbitMessageHeaders.MESSAGE_COUNT, value);
        }

        public string MessageId
        {
            get => GetHeader(Messaging.MessageHeaders.ID) as string;
            set => SetHeader(Messaging.MessageHeaders.ID, value);
        }

        public int? Priority
        {
            get => GetHeader(RabbitMessageHeaders.PRIORITY) as int?;
            set => SetHeader(RabbitMessageHeaders.PRIORITY, value);
        }

        public ulong? PublishSequenceNumber
        {
            get => GetHeader(RabbitMessageHeaders.PUBLISH_SEQUENCE_NUMBER) as ulong?;
            set => SetHeader(RabbitMessageHeaders.PUBLISH_SEQUENCE_NUMBER, value);
        }

        public int? ReceivedDelay
        {
            get => GetHeader(RabbitMessageHeaders.RECEIVED_DELAY) as int?;
            set => SetHeader(RabbitMessageHeaders.RECEIVED_DELAY, value);
        }

        public MessageDeliveryMode? ReceivedDeliveryMode
        {
            get => GetHeader(RabbitMessageHeaders.RECEIVED_DELIVERY_MODE) as MessageDeliveryMode?;
            set => SetHeader(RabbitMessageHeaders.RECEIVED_DELIVERY_MODE, value);
        }

        public string ReceivedExchange
        {
            get => GetHeader(RabbitMessageHeaders.RECEIVED_EXCHANGE) as string;
            set => SetHeader(RabbitMessageHeaders.RECEIVED_EXCHANGE, value);
        }

        public string ReceivedRoutingKey
        {
            get => GetHeader(RabbitMessageHeaders.RECEIVED_ROUTING_KEY) as string;
            set => SetHeader(RabbitMessageHeaders.RECEIVED_ROUTING_KEY, value);
        }

        public string ReceivedUserId
        {
            get => GetHeader(RabbitMessageHeaders.RECEIVED_USER_ID) as string;
            set => SetHeader(RabbitMessageHeaders.RECEIVED_USER_ID, value);
        }

        public bool? Redelivered
        {
            get => GetHeader(RabbitMessageHeaders.REDELIVERED) as bool?;
            set => SetHeader(RabbitMessageHeaders.REDELIVERED, value);
        }

        public string ReplyTo
        {
            get => GetHeader(RabbitMessageHeaders.REPLY_TO) as string;
            set => SetHeader(RabbitMessageHeaders.REPLY_TO, value);
        }

        public Address ReplyToAddress
        {
            get
            {
                var result = ReplyTo;
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
            get => GetHeader(RabbitMessageHeaders.TARGET);
            set => SetHeader(RabbitMessageHeaders.TARGET, value);
        }

        public MethodInfo TargetMethod
        {
            get => GetHeader(RabbitMessageHeaders.TARGET_METHOD) as MethodInfo;
            set => SetHeader(RabbitMessageHeaders.TARGET_METHOD, value);
        }

        public new long? Timestamp
        {
            get => base.Timestamp;
            set => SetHeader(Messaging.MessageHeaders.TIMESTAMP, value);
        }

        public string Type
        {
            get => GetHeader(RabbitMessageHeaders.TYPE) as string;
            set => SetHeader(RabbitMessageHeaders.TYPE, value);
        }

        public string UserId
        {
            get => GetHeader(RabbitMessageHeaders.USER_ID) as string;
            set => SetHeader(RabbitMessageHeaders.USER_ID, value);
        }

        public bool? FinalRetryForMessageWithNoId
        {
            get => GetHeader(RabbitMessageHeaders.FINAL_RETRY_FOR_MESSAGE_WITH_NO_ID) as bool?;
            set => SetHeader(RabbitMessageHeaders.FINAL_RETRY_FOR_MESSAGE_WITH_NO_ID, value);
        }

        public bool IsFinalRetryForMessageWithNoId => FinalRetryForMessageWithNoId.HasValue;

        public bool? LastInBatch
        {
            get => GetHeader(RabbitMessageHeaders.LAST_IN_BATCH) as bool?;
            set => SetHeader(RabbitMessageHeaders.LAST_IN_BATCH, value);
        }

        public List<Dictionary<string, object>> GetXDeathHeader() => GetHeader(RabbitMessageHeaders.X_DEATH) as List<Dictionary<string, object>>;

        public override IMessageHeaders ToMessageHeaders() => Messaging.MessageHeaders.From(headers);

        protected new RabbitHeaderAccessor CreateMutableAccessor(IMessage message) => CreateMutableAccessor(message.Headers);

        protected new RabbitHeaderAccessor CreateMutableAccessor(IMessageHeaders messageHeaders)
        {
            if (messageHeaders is not MessageHeaders headers)
            {
                throw new InvalidOperationException("Unable to create mutable accessor, message has no headers or headers are not of type MessageHeaders");
            }

            return new RabbitHeaderAccessor(headers);
        }

        protected override bool IsReadOnly(string headerName) => !headers.IsMutable;

        protected override void VerifyType(string headerName, object headerValue)
        {
            base.VerifyType(headerName, headerValue);
            if (RabbitMessageHeaders.PRIORITY.Equals(headerName) && headerValue is not int)
            {
                throw new ArgumentException("The '" + headerName + "' header value must be an Integer.");
            }
        }

        protected class RabbitAccessorMessageHeaders : AccessorMessageHeaders
        {
            public RabbitAccessorMessageHeaders(MessageHeaderAccessor accessor, MessageHeaders headers)
                : base(accessor, headers)
            {
            }

            public RabbitAccessorMessageHeaders(MessageHeaderAccessor accessor, IDictionary<string, object> headers)
            : base(accessor, headers)
            {
            }

            public new RabbitHeaderAccessor Accessor => accessor as RabbitHeaderAccessor;
        }
    }
}
