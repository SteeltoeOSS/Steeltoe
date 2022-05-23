// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging
{
    public static class Message
    {
        public static IMessage<T> Create<T>(T payload) => (IMessage<T>)Create(payload, typeof(T));

        public static IMessage<T> Create<T>(T payload, IMessageHeaders headers)
        {
            if (headers == null)
            {
                return (IMessage<T>)Create(payload, typeof(T));
            }
            else
            {
                return (IMessage<T>)Create(payload, headers, typeof(T));
            }
        }

        public static IMessage<T> Create<T>(T payload, IDictionary<string, object> headers) => (IMessage<T>)Create(payload, new MessageHeaders(headers, null, null), typeof(T));

        public static IMessage Create(object payload, Type messageType = null)
        {
            var genParamType = GetGenericParamType(payload, messageType);
            var typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

            return (IMessage)Activator.CreateInstance(
                typeToCreate,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { payload },
                null,
                null);
        }

        public static IMessage Create(object payload, IMessageHeaders headers, Type messageType = null)
        {
            var genParamType = GetGenericParamType(payload, messageType);
            var typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

            return (IMessage)Activator.CreateInstance(
                  typeToCreate,
                  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new[] { payload, headers },
                  null,
                  null);
        }

        public static IMessage Create(object payload, IDictionary<string, object> headers, Type messageType = null)
        {
            var genParamType = GetGenericParamType(payload, messageType);
            var typeToCreate = typeof(Message<>).MakeGenericType(genParamType);

            return (IMessage)Activator.CreateInstance(
                  typeToCreate,
                  BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                  null,
                  new[] { payload, headers },
                  null,
                  null);
        }

        private static Type GetGenericParamType(object payload, Type messageType)
        {
            if (payload == null && messageType == null)
            {
                return typeof(object);
            }

            if (messageType != null)
            {
                return messageType;
            }

            return payload.GetType();
        }
    }

    public abstract class AbstractMessage
    {
    }

    public class Message<P> : AbstractMessage, IMessage<P>
    {
        protected readonly P payload;

        protected readonly IMessageHeaders headers;

        protected internal Message(P payload)
            : this(payload, new MessageHeaders())
        {
        }

        protected internal Message(P payload, IDictionary<string, object> headers)
            : this(payload, new MessageHeaders(headers, null, null))
        {
        }

        protected internal Message(P payload, IMessageHeaders headers)
        {
            this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
            this.headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }

        public P Payload => payload;

        public IMessageHeaders Headers => headers;

        object IMessage.Payload => Payload;

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is not Message<P> otherMsg)
            {
                return false;
            }

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
                sb.Append("byte[").Append(arr.Length).Append(']');
            }
            else
            {
                sb.Append(payload);
            }

            sb.Append(", headers=").Append(headers).Append(']');
            return sb.ToString();
        }
    }
}
