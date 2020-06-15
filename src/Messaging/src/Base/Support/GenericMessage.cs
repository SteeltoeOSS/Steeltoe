// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Support
{
#pragma warning disable SA1402 // File may only contain a single type
    public class GenericMessage<T> : GenericMessage, IMessage<T>
#pragma warning restore SA1402 // File may only contain a single type
    {
        public GenericMessage(T payload)
        : this(payload, new MessageHeaders(null))
        {
        }

        public GenericMessage(T payload, IDictionary<string, object> headers)
        : this(payload, new MessageHeaders(headers))
        {
        }

        public GenericMessage(T payload, IMessageHeaders headers)
            : base(payload, headers)
        {
        }

        public new T Payload
        {
            get { return (T)payload; }
        }
    }

    public class GenericMessage : IMessage
    {
        protected readonly object payload;

        protected readonly IMessageHeaders headers;

        public GenericMessage(object payload)
        : this(payload, new MessageHeaders(null))
        {
        }

        public GenericMessage(object payload, IDictionary<string, object> headers)
        : this(payload, new MessageHeaders(headers))
        {
        }

        public GenericMessage(object payload, IMessageHeaders headers)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            this.payload = payload;
            this.headers = headers;
        }

        public object Payload
        {
            get { return payload; }
        }

        public IMessageHeaders Headers
        {
            get { return headers; }
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (!(other is GenericMessage))
            {
                return false;
            }

            var otherMsg = (GenericMessage)other;
            return ObjectUtils.NullSafeEquals(payload, otherMsg.payload) && headers.Equals(otherMsg.headers);
        }

        public override int GetHashCode()
        {
            // Using nullSafeHashCode for proper array hashCode handling
            return (ObjectUtils.NullSafeHashCode(payload) * 23) + headers.GetHashCode();
        }

        public override string ToString()
        {
            var sb = new StringBuilder(GetType().Name);
            sb.Append(" [payload=");
            if (payload is byte[])
            {
                var arr = (byte[])(object)payload;
                sb.Append("byte[").Append(arr.Length).Append("]");
            }
            else
            {
                sb.Append(payload);
            }

            sb.Append(", headers=").Append(headers).Append("]");
            return sb.ToString();
        }
    }
}
