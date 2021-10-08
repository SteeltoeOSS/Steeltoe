// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Support
{
    public class MutableMessage : IMessage
    {
        protected readonly object _payload;

        protected readonly MutableMessageHeaders _headers;

        public MutableMessage(object payload)
            : this(payload, (Dictionary<string, object>)null)
        {
        }

        public MutableMessage(object payload, IDictionary<string, object> headers)
        : this(payload, new MutableMessageHeaders(headers))
        {
        }

        public MutableMessage(object payload, MutableMessageHeaders headers)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            _payload = payload;
            _headers = headers;
        }

        public IMessageHeaders Headers
        {
            get { return _headers; }
        }

        public object Payload
        {
            get { return _payload; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(GetType().Name);
            sb.Append(" [payload=");
            if (_payload is byte[])
            {
                sb.Append("byte[").Append(((byte[])(object)_payload).Length).Append(']');
            }
            else
            {
                sb.Append(_payload);
            }

            sb.Append(", headers=").Append(_headers).Append(']');
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            return (_headers.GetHashCode() * 23) + ObjectUtils.NullSafeHashCode(_payload);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is MutableMessage other)
            {
                var thisId = _headers.Id;
                var otherId = other._headers.Id;
                return ObjectUtils.NullSafeEquals(thisId, otherId) &&
                        _headers.Equals(other._headers) && _payload.Equals(other._payload);
            }

            return false;
        }

        protected internal IDictionary<string, object> RawHeaders
        {
            get { return _headers.RawHeaders; }
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class MutableMessage<T> : MutableMessage, IMessage<T>
#pragma warning restore SA1402 // File may only contain a single type
    {
        public MutableMessage(T payload)
            : this(payload, (Dictionary<string, object>)null)
        {
        }

        public MutableMessage(T payload, IDictionary<string, object> headers)
            : this(payload, new MutableMessageHeaders(headers))
        {
        }

        public MutableMessage(T payload, MutableMessageHeaders headers)
            : base(payload, headers)
        {
        }

        public new T Payload
        {
            get { return (T)_payload; }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is MutableMessage<T> other)
            {
                var thisId = _headers.Id;
                var otherId = other._headers.Id;
                return ObjectUtils.NullSafeEquals(thisId, otherId) &&
                        _headers.Equals(other._headers) && _payload.Equals(other._payload);
            }

            return false;
        }
    }
}
