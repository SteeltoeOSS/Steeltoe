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

using Steeltoe.Integration.Acks;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration
{
    public class IntegrationMessageHeaderAccessor : MessageHeaderAccessor
    {
        public const string CORRELATION_ID = "correlationId";

        public const string EXPIRATION_DATE = "expirationDate";

        public const string PRIORITY = "priority";

        public const string SEQUENCE_NUMBER = "sequenceNumber";

        public const string SEQUENCE_SIZE = "sequenceSize";

        public const string SEQUENCE_DETAILS = "sequenceDetails";

        public const string ROUTING_SLIP = "routingSlip";

        public const string DUPLICATE_MESSAGE = "duplicateMessage";

        public const string CLOSEABLE_RESOURCE = "closeableResource";

        public const string DELIVERY_ATTEMPT = "deliveryAttempt";

        public const string ACKNOWLEDGMENT_CALLBACK = "acknowledgmentCallback";

        private ISet<string> readOnlyHeaders = new HashSet<string>();

        public IntegrationMessageHeaderAccessor(IMessage message)
        : base(message)
        {
        }

        public void SetReadOnlyHeaders(IList<string> readOnlyHeaders)
        {
            foreach (var h in readOnlyHeaders)
            {
                if (h == null)
                {
                    throw new ArgumentException("'readOnlyHeaders' must not be contain null items.");
                }
            }

            if (readOnlyHeaders.Count > 0)
            {
                this.readOnlyHeaders = new HashSet<string>(readOnlyHeaders);
            }
        }

        public long? GetExpirationDate()
        {
            return (long?)GetHeader(EXPIRATION_DATE);
        }

        public object GetCorrelationId()
        {
            return GetHeader(CORRELATION_ID);
        }

        public int GetSequenceNumber()
        {
            var sequenceNumber = (int?)GetHeader(SEQUENCE_NUMBER);
            return sequenceNumber ?? 0;
        }

        public int GetSequenceSize()
        {
            var sequenceSize = (int?)GetHeader(SEQUENCE_SIZE);
            return sequenceSize ?? 0;
        }

        public int? GetPriority()
        {
            return (int?)GetHeader(PRIORITY);
        }

        public IAcknowledgmentCallback GetAcknowledgmentCallback()
        {
            return (IAcknowledgmentCallback)GetHeader(ACKNOWLEDGMENT_CALLBACK);
        }

        public int? GetDeliveryAttempt()
        {
            return (int?)GetHeader(DELIVERY_ATTEMPT);
        }

        public T GetHeader<T>(string key)
        {
            var value = GetHeader(key);
            if (value == null)
            {
                return default;
            }

            if (!typeof(T).IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException("Incorrect type specified for header '" + key + "'. Expected [" + typeof(T)
                        + "] but actual type is [" + value.GetType() + "]");
            }

            return (T)value;
        }

        public override IDictionary<string, object> ToDictionary()
        {
            if (readOnlyHeaders.Count == 0)
            {
                return base.ToDictionary();
            }
            else
            {
                var headers = base.ToDictionary();
                foreach (var header in readOnlyHeaders)
                {
                    headers.Remove(header);
                }

                return headers;
            }
        }

        protected internal override bool IsReadOnly(string headerName)
        {
            return base.IsReadOnly(headerName) || readOnlyHeaders.Contains(headerName);
        }

        protected internal override void VerifyType(string headerName, object headerValue)
        {
            if (headerName != null && headerValue != null)
            {
                base.VerifyType(headerName, headerValue);
                if (EXPIRATION_DATE.Equals(headerName))
                {
                    if (!(headerValue is DateTime || headerValue is long))
                    {
                        throw new ArgumentException("The '" + headerName + "' header value must be a Date or Long.");
                    }
                }
                else if (SEQUENCE_NUMBER.Equals(headerName)
                        || SEQUENCE_SIZE.Equals(headerName)
                        || PRIORITY.Equals(headerName))
                {
                    if (!(headerValue is int))
                    {
                        throw new ArgumentException("The '" + headerName + "' header value must be a integer.");
                    }
                }
                else if (ROUTING_SLIP.Equals(headerName))
                {
                    if (!typeof(IDictionary<string, object>).IsAssignableFrom(headerValue.GetType()))
                    {
                        throw new ArgumentException("The '" + headerName + "' header value must be a IDictionary<string,object>.");
                    }
                }
                else if (DUPLICATE_MESSAGE.Equals(headerName) && !(headerValue is bool))
                {
                    throw new ArgumentException("The '" + headerName + "' header value must be a boolean.");
                }
            }
        }
    }
}
