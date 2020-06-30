// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Messaging
{
    public class MessageHeaders : IMessageHeaders
    {
        public const string ID = "id";
        public const string TIMESTAMP = "timestamp";
        public const string CONTENT_TYPE = "contentType";
        public const string REPLY_CHANNEL = "replyChannel";
        public const string ERROR_CHANNEL = "errorChannel";
        public const string INFERRED_ARGUMENT_TYPE = "InferredArgumentType";

        public const string CONTENT_TYPE_JSON = "application/json";
        public const string CONTENT_TYPE_TEXT_PLAIN = "text/plain";
        public const string CONTENT_TYPE_BYTES = "application/octet-stream";
        public const string CONTENT_TYPE_JSON_ALT = "text/x-json";
        public const string CONTENT_TYPE_XML = "application/xml";
        public const string CONTENT_TYPE_JAVA_SERIALIZED_OBJECT = "application/x-java-serialized-object";
        public const string CONTENT_TYPE_DOTNET_SERIALIZED_OBJECT = "application/x-dotnet-serialized-object";

        public static readonly string ID_VALUE_NONE = string.Empty;
        protected readonly IDictionary<string, object> headers;

        private static readonly IIDGenerator _defaultIdGenerator = new DefaultIdGenerator();
        private static volatile IIDGenerator _idGenerator;

        public MessageHeaders(IDictionary<string, object> headers = null)
        : this(headers, null, null)
        {
        }

        public MessageHeaders(IDictionary<string, object> headers, string id, long? timestamp)
        {
            this.headers = headers != null ? new Dictionary<string, object>(headers) : new Dictionary<string, object>();
            UpdateHeaders(id, timestamp);
        }

        public static MessageHeaders From(MessageHeaders other)
        {
            return new MessageHeaders(other);
        }

        protected MessageHeaders(MessageHeaders other)
        {
            headers = other.RawHeaders;
        }

        public virtual string Id
        {
            get
            {
                if (!headers.TryGetValue(ID, out var result))
                {
                    return null;
                }

                return result.ToString();
            }
        }

        public virtual long? Timestamp
        {
            get
            {
                if (!headers.TryGetValue(TIMESTAMP, out var result))
                {
                    return null;
                }

                return (long)result;
            }
        }

        public virtual object ReplyChannel
        {
            get
            {
                headers.TryGetValue(REPLY_CHANNEL, out var chan);
                return chan;
            }
        }

        public virtual object ErrorChannel
        {
            get
            {
                headers.TryGetValue(ERROR_CHANNEL, out var chan);
                return chan;
            }
        }

        public override bool Equals(object other)
        {
            return this == other ||
                    (other is MessageHeaders && ContentsEqual(((MessageHeaders)other).headers));
        }

        public override int GetHashCode()
        {
            return headers.GetHashCode();
        }

        public override string ToString()
        {
            return headers.ToString();
        }

        public virtual bool ContainsKey(string key)
        {
            return headers.ContainsKey(key);
        }

        public virtual bool TryGetValue(string key, out object value)
        {
            return headers.TryGetValue(key, out value);
        }

        public virtual void Add(string key, object value)
        {
            throw new InvalidOperationException();
        }

        public virtual void Add(KeyValuePair<string, object> item)
        {
            throw new InvalidOperationException();
        }

        public virtual void Clear()
        {
            throw new InvalidOperationException();
        }

        public virtual bool Contains(KeyValuePair<string, object> item)
        {
            return headers.Contains(item);
        }

        public virtual void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            headers.CopyTo(array, arrayIndex);
        }

        public virtual bool Remove(string key)
        {
            throw new InvalidOperationException();
        }

        public virtual bool Remove(KeyValuePair<string, object> item)
        {
            throw new InvalidOperationException();
        }

        public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return headers.GetEnumerator();
        }

        public virtual ICollection<string> Keys => headers.Keys;

        public virtual ICollection<object> Values => headers.Values;

        public virtual int Count => headers.Count;

        public virtual bool IsReadOnly => true;

        public virtual object this[string key] { get => Get<object>(key); set => throw new InvalidOperationException(); }

        public virtual T Get<T>(string key)
        {
            if (TryGetValue(key, out var val))
            {
                return (T)val;
            }

            return default;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)headers).GetEnumerator();
        }

        internal bool ContentsEqual(IDictionary<string, object> other)
        {
            if (other == null)
            {
                return false;
            }

            if (headers.Count != other.Count)
            {
                return false;
            }

            foreach (var entry in headers)
            {
                if (other.TryGetValue(entry.Key, out var ovalue))
                {
                    if (ovalue != entry.Value)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        internal static IIDGenerator IdGenerator
        {
            get
            {
                var generator = _idGenerator;
                return generator != null ? generator : _defaultIdGenerator;
            }

            set
            {
                _idGenerator = value;
            }
        }

        protected void UpdateHeaders(string id, long? timestamp)
        {
            if (id == null)
            {
                headers[ID] = IdGenerator.GenerateId().ToString();
            }
            else if (id == ID_VALUE_NONE)
            {
                headers.Remove(ID);
            }
            else
            {
                headers[ID] = id;
            }

            if (timestamp == null)
            {
                headers[TIMESTAMP] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else if (timestamp.Value < 0)
            {
                headers.Remove(TIMESTAMP);
            }
            else
            {
                headers[TIMESTAMP] = timestamp.Value;
            }
        }

        protected internal virtual IDictionary<string, object> RawHeaders
        {
            get { return headers; }
        }
    }
}
