// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public const string SOURCE_DATA = "sourceData";

        private ISet<string> _readOnlyHeaders = new HashSet<string>();

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
                _readOnlyHeaders = new HashSet<string>(readOnlyHeaders);
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
            if (_readOnlyHeaders.Count == 0)
            {
                return base.ToDictionary();
            }
            else
            {
                var headers = base.ToDictionary();
                foreach (var header in _readOnlyHeaders)
                {
                    headers.Remove(header);
                }

                return headers;
            }
        }

        public new bool IsReadOnly(string headerName)
        {
            return base.IsReadOnly(headerName) || _readOnlyHeaders.Contains(headerName);
        }

        protected override void VerifyType(string headerName, object headerValue)
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
