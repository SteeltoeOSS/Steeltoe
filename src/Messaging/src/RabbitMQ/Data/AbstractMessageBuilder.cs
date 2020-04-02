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

namespace Steeltoe.Messaging.Rabbit.Data
{
    public abstract class AbstractMessageBuilder<T>
    {
        protected AbstractMessageBuilder()
        {
        }

        protected AbstractMessageBuilder(MessageProperties properties)
        {
            Properties = properties;
        }

        protected virtual MessageProperties Properties { get; set; } = new MessageProperties();

        public virtual AbstractMessageBuilder<T> SetHeader(string key, object value)
        {
            Properties.SetHeader(key, value);
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetTimestamp(DateTime timestamp)
        {
            Properties.Timestamp = timestamp;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetMessageId(string messageId)
        {
            Properties.MessageId = messageId;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetUserId(string userId)
        {
            Properties.UserId = userId;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetAppId(string appId)
        {
            Properties.AppId = appId;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetClusterId(string clusterId)
        {
            Properties.ClusterId = clusterId;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetType(string type)
        {
            Properties.Type = type;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetCorrelationId(string correlationId)
        {
            Properties.CorrelationId = correlationId;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReplyTo(string replyTo)
        {
            Properties.ReplyTo = replyTo;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReplyToAddress(Address replyTo)
        {
            Properties.ReplyToAddress = replyTo;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentType(string contentType)
        {
            Properties.ContentType = contentType;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentEncoding(string contentEncoding)
        {
            Properties.ContentEncoding = contentEncoding;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentLength(long contentLength)
        {
            Properties.ContentLength = contentLength;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetDeliveryMode(MessageDeliveryMode deliveryMode)
        {
            Properties.DeliveryMode = deliveryMode;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetExpiration(string expiration)
        {
            Properties.Expiration = expiration;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetPriority(int priority)
        {
            Properties.Priority = priority;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReceivedExchange(string receivedExchange)
        {
            Properties.ReceivedExchange = receivedExchange;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReceivedRoutingKey(string receivedRoutingKey)
        {
            Properties.ReceivedRoutingKey = receivedRoutingKey;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetRedelivered(bool redelivered)
        {
            Properties.Redelivered = redelivered;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetDeliveryTag(ulong deliveryTag)
        {
            Properties.DeliveryTag = deliveryTag;
            return this;
        }

        public virtual AbstractMessageBuilder<T> SetMessageCount(uint messageCount)
        {
            Properties.MessageCount = messageCount;
            return this;
        }

        /*
         * *ifAbsent variants...
         */

        public virtual AbstractMessageBuilder<T> SetHeaderIfAbsent(string key, object value)
        {
            if (!Properties.Headers.ContainsKey(key))
            {
                Properties.Headers.Add(key, value);
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetTimestampIfAbsent(DateTime timestamp)
        {
            if (!Properties.Timestamp.HasValue)
            {
                Properties.Timestamp = timestamp;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetMessageIdIfAbsent(string messageId)
        {
            if (Properties.MessageId == null)
            {
                Properties.MessageId = messageId;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetUserIdIfAbsent(string userId)
        {
            if (Properties.UserId == null)
            {
                Properties.UserId = userId;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetAppIdIfAbsent(string appId)
        {
            if (Properties.AppId == null)
            {
                Properties.AppId = appId;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetClusterIdIfAbsent(string clusterId)
        {
            if (Properties.ClusterId == null)
            {
                Properties.ClusterId = clusterId;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetTypeIfAbsent(string type)
        {
            if (Properties.Type == null)
            {
                Properties.Type = type;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetCorrelationIdIfAbsent(string correlationId)
        {
            if (Properties.CorrelationId == null)
            {
                Properties.CorrelationId = correlationId;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReplyToIfAbsent(string replyTo)
        {
            if (Properties.ReplyTo == null)
            {
                Properties.ReplyTo = replyTo;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReplyToAddressIfAbsent(Address replyTo)
        {
            if (Properties.ReplyToAddress == null)
            {
                Properties.ReplyToAddress = replyTo;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentTypeIfAbsentOrDefault(string contentType)
        {
            if (Properties.ContentType == null
                    || Properties.ContentType.Equals(MessageProperties.DEFAULT_CONTENT_TYPE))
            {
                Properties.ContentType = contentType;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentEncodingIfAbsent(string contentEncoding)
        {
            if (Properties.ContentEncoding == null)
            {
                Properties.ContentEncoding = contentEncoding;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetContentLengthIfAbsent(long contentLength)
        {
            if (!Properties.IsContentLengthSet)
            {
                Properties.ContentLength = contentLength;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetDeliveryModeIfAbsentOrDefault(MessageDeliveryMode deliveryMode)
        {
            if (Properties.DeliveryMode == null
                    || Properties.DeliveryMode.Equals(MessageProperties.DEFAULT_DELIVERY_MODE))
            {
                Properties.DeliveryMode = deliveryMode;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetExpirationIfAbsent(string expiration)
        {
            if (Properties.Expiration == null)
            {
                Properties.Expiration = expiration;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetPriorityIfAbsentOrDefault(int priority)
        {
            if (Properties.Priority == null
                    || Properties.Priority == MessageProperties.DEFAULT_PRIORITY)
            {
                Properties.Priority = priority;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReceivedExchangeIfAbsent(string receivedExchange)
        {
            if (Properties.ReceivedExchange == null)
            {
                Properties.ReceivedExchange = receivedExchange;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetReceivedRoutingKeyIfAbsent(string receivedRoutingKey)
        {
            if (Properties.ReceivedRoutingKey == null)
            {
                Properties.ReceivedRoutingKey = receivedRoutingKey;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetRedeliveredIfAbsent(bool redelivered)
        {
            if (Properties.Redelivered == null)
            {
                Properties.Redelivered = redelivered;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetDeliveryTagIfAbsent(ulong deliveryTag)
        {
            if (!Properties.IsDeliveryTagSet)
            {
                Properties.DeliveryTag = deliveryTag;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> SetMessageCountIfAbsent(uint messageCount)
        {
            if (Properties.MessageCount == null)
            {
                Properties.MessageCount = messageCount;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> CopyProperties(MessageProperties properties)
        {
            Properties.Copy(properties);

            // Special handling of replyTo needed because the format depends on how it was Set
            Properties.ReplyTo = properties.ReplyTo;
            return this;
        }

        public virtual AbstractMessageBuilder<T> CopyHeaders(Dictionary<string, object> headers)
        {
            foreach (var entry in headers)
            {
                Properties.Headers[entry.Key] = entry.Value;
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> CopyHeadersIfAbsent(Dictionary<string, object> headers)
        {
            foreach (var entry in headers)
            {
                if (!Properties.Headers.ContainsKey(entry.Key))
                {
                    Properties.Headers[entry.Key] = entry.Value;
                }
            }

            return this;
        }

        public virtual AbstractMessageBuilder<T> RemoveHeader(string key)
        {
            Properties.Headers.Remove(key);
            return this;
        }

        public virtual AbstractMessageBuilder<T> RemoveHeaders()
        {
            Properties.Headers.Clear();
            return this;
        }

        public abstract T Build();
    }
}
