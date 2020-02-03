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
