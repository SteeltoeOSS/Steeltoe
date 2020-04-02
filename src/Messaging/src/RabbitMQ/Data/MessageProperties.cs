// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Data
{
    // TODO: This is an AMQP Type
    public class MessageProperties
    {
        public const int INT_MASK = 32;

        public const string CONTENT_TYPE_BYTES = "application/octet-stream";

        public const string CONTENT_TYPE_TEXT_PLAIN = "text/plain";

        public const string CONTENT_TYPE_SERIALIZED_OBJECT = "application/x-java-serialized-object";

        public const string CONTENT_TYPE_JSON = "application/json";

        public const string CONTENT_TYPE_JSON_ALT = "text/x-json";

        public const string CONTENT_TYPE_XML = "application/xml";

        public const string SPRING_BATCH_FORMAT = "springBatchFormat";

        public const string BATCH_FORMAT_LENGTH_HEADER4 = "lengthHeader4";

        public const string SPRING_AUTO_DECOMPRESS = "springAutoDecompress";

        public const string X_DELAY = "x-delay";

        public const string DEFAULT_CONTENT_TYPE = CONTENT_TYPE_BYTES;

        public const MessageDeliveryMode DEFAULT_DELIVERY_MODE = MessageDeliveryMode.PERSISTENT;

        public const int DEFAULT_PRIORITY = 0;

        public string AppId { get; set; }

        public string ClusterId { get; set; }

        public string ConsumerQueue { get; set; }

        public string ConsumerTag { get; set; }

        public string ContentEncoding { get; set; }

        public long? ContentLength { get; set; }

        public bool IsContentLengthSet => ContentLength.HasValue;

        public string ContentType { get; set; }

        public string CorrelationId { get; set; }

        public int? Delay
        {
            get
            {
                Headers.TryGetValue(X_DELAY, out var delay);
                return delay as int?;
            }

            set
            {
                if (value == null || value.Value < 0)
                {
                    if (Headers.ContainsKey(X_DELAY))
                    {
                        Headers.Remove(X_DELAY);
                    }
                }
                else
                {
                    Headers[X_DELAY] = value;
                }
            }
        }

        public MessageDeliveryMode? DeliveryMode { get; set; }

        public ulong? DeliveryTag { get; set; }

        public bool IsDeliveryTagSet => DeliveryTag.HasValue;

        public string Expiration { get; set; }

        public IDictionary<string, object> Headers { get; internal set; } = new Dictionary<string, object>();

        public Type InferredArgumentType { get; set; }

        public uint? MessageCount { get; set; }

        public string MessageId { get; set; }

        public int? Priority { get; set; }

        public ulong PublishSequenceNumber { get; set; }

        public int? ReceivedDelay { get; set; }

        public MessageDeliveryMode? ReceivedDeliveryMode { get; set; }

        public string ReceivedExchange { get; set; }

        public string ReceivedRoutingKey { get; set; }

        public string ReceivedUserId { get; set; }

        public bool? Redelivered { get; set; }

        public string ReplyTo { get; set; }

        public Address ReplyToAddress { get; set; }

        public object Target { get; set; }

        public MethodInfo TargetMethod { get; set; }

        public DateTime? Timestamp { get; set; }

        public string Type { get; set; }

        public string UserId { get; set; }

        public bool? FinalRetryForMessageWithNoId { get; set; }

        public bool IsFinalRetryForMessageWithNoId => FinalRetryForMessageWithNoId.HasValue;

        public bool LastInBatch { get; set; }

        public List<Dictionary<string, object>> GetXDeathHeader()
        {
            Headers.TryGetValue("x-death", out var result);
            var list = result as List<Dictionary<string, object>>;
            return list;
        }

        public void SetHeader(string key, object value)
        {
            Headers[key] = value;
        }

        public T GetHeader<T>(string headerName)
        {
            Headers.TryGetValue(headerName, out var result);
            return (T)result;
        }

        public override int GetHashCode()
        {
            var prime = 31;
            var result = 1;
            result = (prime * result) + ((AppId == null) ? 0 : AppId.GetHashCode());
            result = (prime * result) + ((ClusterId == null) ? 0 : ClusterId.GetHashCode());
            result = (prime * result) + ((ContentEncoding == null) ? 0 : ContentEncoding.GetHashCode());
            result = (prime * result) + (int)(ContentLength ^ (ContentLength >> INT_MASK));
            result = (prime * result) + ((ContentType == null) ? 0 : ContentType.GetHashCode());
            result = (prime * result) + ((CorrelationId == null) ? 0 : CorrelationId.GetHashCode());
            result = (prime * result) + ((DeliveryMode == null) ? 0 : DeliveryMode.GetHashCode());
            result = (prime * result) + (int)(DeliveryTag ^ (DeliveryTag >> INT_MASK));
            result = (prime * result) + ((Expiration == null) ? 0 : Expiration.GetHashCode());
            result = (prime * result) + Headers.GetHashCode();
            result = (prime * result) + ((MessageCount == null) ? 0 : MessageCount.GetHashCode());
            result = (prime * result) + ((MessageId == null) ? 0 : MessageId.GetHashCode());
            result = (prime * result) + ((Priority == null) ? 0 : Priority.GetHashCode());
            result = (prime * result) + ((ReceivedExchange == null) ? 0 : ReceivedExchange.GetHashCode());
            result = (prime * result) + ((ReceivedRoutingKey == null) ? 0 : ReceivedRoutingKey.GetHashCode());
            result = (prime * result) + ((Redelivered == null) ? 0 : Redelivered.GetHashCode());
            result = (prime * result) + ((ReplyTo == null) ? 0 : ReplyTo.GetHashCode());
            result = (prime * result) + ((Timestamp == null) ? 0 : Timestamp.GetHashCode());
            result = (prime * result) + ((Type == null) ? 0 : Type.GetHashCode());
            result = (prime * result) + ((UserId == null) ? 0 : UserId.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            var other = (MessageProperties)obj;
            if (AppId == null)
            {
                if (other.AppId != null)
                {
                    return false;
                }
            }
            else if (!AppId.Equals(other.AppId))
            {
                return false;
            }

            if (ClusterId == null)
            {
                if (other.ClusterId != null)
                {
                    return false;
                }
            }
            else if (!ClusterId.Equals(other.ClusterId))
            {
                return false;
            }

            if (ContentEncoding == null)
            {
                if (other.ContentEncoding != null)
                {
                    return false;
                }
            }
            else if (!ContentEncoding.Equals(other.ContentEncoding))
            {
                return false;
            }

            if (ContentLength != other.ContentLength)
            {
                return false;
            }

            if (ContentType == null)
            {
                if (other.ContentType != null)
                {
                    return false;
                }
            }
            else if (!ContentType.Equals(other.ContentType))
            {
                return false;
            }

            if (CorrelationId == null)
            {
                if (other.CorrelationId != null)
                {
                    return false;
                }
            }
            else if (!CorrelationId.Equals(other.CorrelationId))
            {
                return false;
            }

            if (DeliveryMode != other.DeliveryMode)
            {
                return false;
            }

            if (DeliveryTag != other.DeliveryTag)
            {
                return false;
            }

            if (Expiration == null)
            {
                if (other.Expiration != null)
                {
                    return false;
                }
            }
            else if (!Expiration.Equals(other.Expiration))
            {
                return false;
            }

            if (!Headers.Equals(other.Headers))
            {
                return false;
            }

            if (MessageCount == null)
            {
                if (other.MessageCount != null)
                {
                    return false;
                }
            }
            else if (!MessageCount.Equals(other.MessageCount))
            {
                return false;
            }

            if (MessageId == null)
            {
                if (other.MessageId != null)
                {
                    return false;
                }
            }
            else if (!MessageId.Equals(other.MessageId))
            {
                return false;
            }

            if (Priority == null)
            {
                if (other.Priority != null)
                {
                    return false;
                }
            }
            else if (!Priority.Equals(other.Priority))
            {
                return false;
            }

            if (ReceivedExchange == null)
            {
                if (other.ReceivedExchange != null)
                {
                    return false;
                }
            }
            else if (!ReceivedExchange.Equals(other.ReceivedExchange))
            {
                return false;
            }

            if (ReceivedRoutingKey == null)
            {
                if (other.ReceivedRoutingKey != null)
                {
                    return false;
                }
            }
            else if (!ReceivedRoutingKey.Equals(other.ReceivedRoutingKey))
            {
                return false;
            }

            if (Redelivered == null)
            {
                if (other.Redelivered != null)
                {
                    return false;
                }
            }
            else if (!Redelivered.Equals(other.Redelivered))
            {
                return false;
            }

            if (ReplyTo == null)
            {
                if (other.ReplyTo != null)
                {
                    return false;
                }
            }
            else if (!ReplyTo.Equals(other.ReplyTo))
            {
                return false;
            }

            if (Timestamp == null)
            {
                if (other.Timestamp != null)
                {
                    return false;
                }
            }
            else if (!Timestamp.Equals(other.Timestamp))
            {
                return false;
            }

            if (Type == null)
            {
                if (other.Type != null)
                {
                    return false;
                }
            }
            else if (!Type.Equals(other.Type))
            {
                return false;
            }

            if (UserId == null)
            {
                if (other.UserId != null)
                {
                    return false;
                }
            }
            else if (!UserId.Equals(other.UserId))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return "MessageProperties [Headers=" + Headers
                    + (Timestamp == null ? string.Empty : ", Timestamp=" + Timestamp)
                    + (MessageId == null ? string.Empty : ", MessageId=" + MessageId)
                    + (UserId == null ? string.Empty : ", UserId=" + UserId)
                    + (ReceivedUserId == null ? string.Empty : ", receivedUserId=" + ReceivedUserId)
                    + (AppId == null ? string.Empty : ", AppId=" + AppId)
                    + (ClusterId == null ? string.Empty : ", ClusterId=" + ClusterId)
                    + (Type == null ? string.Empty : ", Type=" + Type)
                    + (CorrelationId == null ? string.Empty : ", CorrelationId=" + CorrelationId)
                    + (ReplyTo == null ? string.Empty : ", ReplyTo=" + ReplyTo)
                    + (ContentType == null ? string.Empty : ", ContentType=" + ContentType)
                    + (ContentEncoding == null ? string.Empty : ", ContentEncoding=" + ContentEncoding)
                    + ", ContentLength=" + ContentLength
                    + (DeliveryMode == null ? string.Empty : ", DeliveryMode=" + DeliveryMode)
                    + (ReceivedDeliveryMode == null ? string.Empty : ", receivedDeliveryMode=" + ReceivedDeliveryMode)
                    + (Expiration == null ? string.Empty : ", Expiration=" + Expiration)
                    + (Priority == null ? string.Empty : ", Priority=" + Priority)
                    + (Redelivered == null ? string.Empty : ", Redelivered=" + Redelivered)
                    + (ReceivedExchange == null ? string.Empty : ", ReceivedExchange=" + ReceivedExchange)
                    + (ReceivedRoutingKey == null ? string.Empty : ", ReceivedRoutingKey=" + ReceivedRoutingKey)
                    + (ReceivedDelay == null ? string.Empty : ", receivedDelay=" + ReceivedDelay)
                    + ", DeliveryTag=" + DeliveryTag
                    + (MessageCount == null ? string.Empty : ", MessageCount=" + MessageCount)
                    + (ConsumerTag == null ? string.Empty : ", consumerTag=" + ConsumerTag)
                    + (ConsumerQueue == null ? string.Empty : ", consumerQueue=" + ConsumerQueue)
                    + "]";
        }

        public void Copy(MessageProperties source)
        {
            AppId = source.AppId;
            ClusterId = source.ClusterId;
            ConsumerQueue = source.ConsumerQueue;
            ConsumerTag = source.ConsumerTag;
            ContentEncoding = source.ContentEncoding;
            ContentLength = source.ContentLength;
            ContentType = source.ContentType;
            CorrelationId = source.CorrelationId;
            DeliveryMode = source.DeliveryMode;
            DeliveryTag = source.DeliveryTag;
            Expiration = source.Expiration;
            InferredArgumentType = source.InferredArgumentType;
            MessageCount = source.MessageCount;
            Priority = source.Priority;
            PublishSequenceNumber = source.PublishSequenceNumber;
            ReceivedDeliveryMode = source.ReceivedDeliveryMode;
            ReceivedExchange = source.ReceivedExchange;
            ReceivedRoutingKey = source.ReceivedRoutingKey;
            ReceivedUserId = source.ReceivedUserId;
            Redelivered = source.Redelivered;
            ReplyTo = source.ReplyTo;
            ReplyToAddress = source.ReplyToAddress;
            Target = source.Target;
            TargetMethod = source.TargetMethod;
            Timestamp = source.Timestamp;
            Type = source.Type;
            UserId = source.UserId;
            FinalRetryForMessageWithNoId = source.FinalRetryForMessageWithNoId;
            LastInBatch = source.LastInBatch;
            foreach (var entry in source.Headers)
            {
                Headers[entry.Key] = entry.Value;
            }
        }
    }
}
