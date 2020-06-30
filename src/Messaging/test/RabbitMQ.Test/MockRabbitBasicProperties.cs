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

using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit
{
    public class MockRabbitBasicProperties : IBasicProperties
    {
        public string AppId { get; set; }

        public string ClusterId { get; set; }

        public string ContentEncoding { get; set; }

        public string ContentType { get; set; }

        public string CorrelationId { get; set; }

        public byte DeliveryMode { get; set; } = 0xff;

        public string Expiration { get; set; }

        public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();

        public string MessageId { get; set; }

        public bool Persistent { get; set; }

        public byte Priority { get; set; } = 0xff;

        public string ReplyTo { get; set; }

        public PublicationAddress ReplyToAddress { get; set; }

        public AmqpTimestamp Timestamp { get; set; }

        public string Type { get; set; }

        public string UserId { get; set; }

        ushort IContentHeader.ProtocolClassId => 0;

        string IContentHeader.ProtocolClassName => string.Empty;

        public ushort ProtocolClassId;

        public string ProtocolClassName;

        public void ClearAppId()
        {
        }

        public void ClearClusterId()
        {
        }

        public void ClearContentEncoding()
        {
        }

        public void ClearContentType()
        {
        }

        public void ClearCorrelationId()
        {
        }

        public void ClearDeliveryMode()
        {
        }

        public void ClearExpiration()
        {
        }

        public void ClearHeaders()
        {
        }

        public void ClearMessageId()
        {
        }

        public void ClearPriority()
        {
        }

        public void ClearReplyTo()
        {
        }

        public void ClearTimestamp()
        {
        }

        public void ClearType()
        {
        }

        public void ClearUserId()
        {
        }

        public bool IsAppIdPresent()
        {
            return AppId != null;
        }

        public bool IsClusterIdPresent()
        {
            return ClusterId != null;
        }

        public bool IsContentEncodingPresent()
        {
            return ContentEncoding != null;
        }

        public bool IsContentTypePresent()
        {
            return ContentType != null;
        }

        public bool IsCorrelationIdPresent()
        {
            return CorrelationId != null;
        }

        public bool IsDeliveryModePresent()
        {
            return DeliveryMode != 0xff;
        }

        public bool IsExpirationPresent()
        {
            return Expiration != null;
        }

        public bool IsHeadersPresent()
        {
            return Headers != null;
        }

        public bool IsMessageIdPresent()
        {
            return MessageId != null;
        }

        public bool IsPriorityPresent()
        {
            return Priority != 0xff;
        }

        public bool IsReplyToPresent()
        {
            return ReplyTo != null;
        }

        public bool IsTimestampPresent()
        {
            return !default(AmqpTimestamp).Equals(Timestamp);
        }

        public bool IsTypePresent()
        {
            return Type != null;
        }

        public bool IsUserIdPresent()
        {
            return UserId != null;
        }
    }
}
