// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.Support
{
    public class MessageHeaderAccessor : IMessageHeaderAccessor
    {
        public static readonly Encoding DEFAULT_CHARSET = Encoding.UTF8;

        protected AccessorMessageHeaders headers;

        private static readonly MimeType[] READABLE_MIME_TYPES = new MimeType[]
        {
            MimeTypeUtils.APPLICATION_JSON,
            MimeTypeUtils.APPLICATION_XML,
            new MimeType("text", "*"),
            new MimeType("application", "*+json"),
            new MimeType("application", "*+xml")
        };

        private bool _leaveMutable;

        public MessageHeaderAccessor()
        : this((IMessage)null)
        {
        }

        public MessageHeaderAccessor(IMessage message)
        {
            headers = new AccessorMessageHeaders(this, message?.Headers);
        }

        public MessageHeaderAccessor(MessageHeaders headers)
        {
            this.headers = new AccessorMessageHeaders(this, headers);
        }

        public static T GetAccessor<T>(IMessage message)
            where T : MessageHeaderAccessor
        {
            return (T)GetAccessor(message.Headers, typeof(T));
        }

        public static MessageHeaderAccessor GetAccessor(IMessage message, Type accessorType)
        => GetAccessor(message.Headers, accessorType);

        public static T GetAccessor<T>(IMessageHeaders messageHeaders)
            where T : MessageHeaderAccessor
        {
            return (T)GetAccessor(messageHeaders, typeof(T));
        }

        public static MessageHeaderAccessor GetAccessor(IMessageHeaders messageHeaders, Type accessorType)
        {
            if (messageHeaders is AccessorMessageHeaders accessorMessageHeaders)
            {
                var headerAccessor = accessorMessageHeaders.Accessor;
                if (accessorType == null || accessorType.IsInstanceOfType(headerAccessor))
                {
                    return headerAccessor;
                }
            }

            return null;
        }

        public static T GetMutableAccessor<T>(IMessage message)
            where T : MessageHeaderAccessor
        {
            return (T)GetMutableAccessor(message, typeof(T));
        }

        public static T GetMutableAccessor<T>(IMessageHeaders messageHeaders)
            where T : MessageHeaderAccessor
        {
            return (T)GetMutableAccessor(messageHeaders, typeof(T));
        }

        public static MessageHeaderAccessor GetMutableAccessor(IMessage message, Type accessorType = null)
        => GetMutableAccessor(message.Headers, accessorType);

        public static MessageHeaderAccessor GetMutableAccessor(IMessageHeaders headers, Type accessorType = null)
        {
            MessageHeaderAccessor messageHeaderAccessor = null;
            if (headers is AccessorMessageHeaders accessorMessageHeaders)
            {
                var headerAccessor = accessorMessageHeaders.Accessor;
                if (accessorType == null || accessorType.IsInstanceOfType(headerAccessor))
                {
                    messageHeaderAccessor = headerAccessor.IsMutable ? headerAccessor : headerAccessor.CreateMutableAccessor(headers);
                }
            }

            if (messageHeaderAccessor == null && accessorType == null && headers is MessageHeaders msgHeaders)
            {
                messageHeaderAccessor = new MessageHeaderAccessor(msgHeaders);
            }

            return messageHeaderAccessor;
        }

        public virtual bool EnableTimestamp { get; set; } = false;

        public virtual IIDGenerator IdGenerator { get; set; }

        public virtual bool LeaveMutable
        {
            get => _leaveMutable;

            set
            {
                if (!headers.IsMutable)
                {
                    throw new InvalidOperationException("Already immutable");
                }

                _leaveMutable = value;
            }
        }

        public virtual bool IsMutable => headers.IsMutable;

        public virtual bool IsModified { get; set; }

        public virtual IMessageHeaders MessageHeaders
        {
            get
            {
                if (!_leaveMutable)
                {
                    SetImmutable();
                }

                return headers;
            }
        }

        public virtual string Id
        {
            get
            {
                if (GetHeader(Messaging.MessageHeaders.ID) == null)
                {
                    return null;
                }

                return GetHeader(Messaging.MessageHeaders.ID).ToString();
            }
        }

        public virtual long? Timestamp
        {
            get
            {
                var value = GetHeader(Messaging.MessageHeaders.TIMESTAMP);
                if (value == null)
                {
                    return null;
                }

                return value is long longVal ? longVal : long.Parse(value.ToString());
            }
        }

        public virtual string ContentType
        {
            get
            {
                var value = GetHeader(Messaging.MessageHeaders.CONTENT_TYPE);
                if (value == null)
                {
                    return null;
                }

                return value.ToString();
            }

            set => SetHeader(Messaging.MessageHeaders.CONTENT_TYPE, value);
        }

        public virtual string ReplyChannelName
        {
            get => GetHeader(Messaging.MessageHeaders.REPLY_CHANNEL) as string;
            set => SetHeader(Messaging.MessageHeaders.REPLY_CHANNEL, value);
        }

        public virtual object ReplyChannel
        {
            get => GetHeader(Messaging.MessageHeaders.REPLY_CHANNEL);
            set => SetHeader(Messaging.MessageHeaders.REPLY_CHANNEL, value);
        }

        public virtual string ErrorChannelName
        {
            get => GetHeader(Messaging.MessageHeaders.ERROR_CHANNEL) as string;
            set => SetHeader(Messaging.MessageHeaders.ERROR_CHANNEL, value);
        }

        public virtual object ErrorChannel
        {
            get => GetHeader(Messaging.MessageHeaders.ERROR_CHANNEL);
            set => SetHeader(Messaging.MessageHeaders.ERROR_CHANNEL, value);
        }

        public virtual void SetImmutable() => headers.SetImmutable();

        public virtual IMessageHeaders ToMessageHeaders() => new MessageHeaders(headers);

        public virtual IDictionary<string, object> ToDictionary() => new Dictionary<string, object>(headers);

        // Generic header accessors
        public virtual object GetHeader(string headerName) => headers.TryGetValue(headerName, out var value) ? value : null;

        public virtual void SetHeader(string name, object value)
        {
            if (IsReadOnly(name))
            {
                throw new ArgumentException($"'{name}' header is read-only");
            }

            VerifyType(name, value);

            if (value != null)
            {
                // Modify header if necessary
                if (!ObjectUtils.NullSafeEquals(value, GetHeader(name)))
                {
                    IsModified = true;
                    headers.RawHeaders[name] = value;
                }
            }
            else
            {
                // Remove header if available
                if (headers.ContainsKey(name))
                {
                    IsModified = true;
                    headers.RawHeaders.Remove(name);
                }
            }
        }

        public virtual void SetHeaderIfAbsent(string name, object value)
        {
            if (GetHeader(name) == null)
            {
                SetHeader(name, value);
            }
        }

        public virtual void RemoveHeader(string headerName)
        {
            if (!string.IsNullOrEmpty(headerName) && !IsReadOnly(headerName))
            {
                SetHeader(headerName, null);
            }
        }

        public virtual void RemoveHeaders(params string[] headerPatterns)
        {
            var headersToRemove = new List<string>();
            foreach (var pattern in headerPatterns)
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    if (pattern.Contains("*"))
                    {
                        headersToRemove.AddRange(GetMatchingHeaderNames(pattern, headers));
                    }
                    else
                    {
                        headersToRemove.Add(pattern);
                    }
                }
            }

            foreach (var headerToRemove in headersToRemove)
            {
                RemoveHeader(headerToRemove);
            }
        }

        public virtual void CopyHeaders(IDictionary<string, object> headersToCopy)
        {
            if (headersToCopy != null)
            {
                foreach (var kvp in headersToCopy)
                {
                    if (!IsReadOnly(kvp.Key))
                    {
                        SetHeader(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        public virtual void CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
        {
            if (headersToCopy != null)
            {
                foreach (var kvp in headersToCopy)
                {
                    if (!IsReadOnly(kvp.Key))
                    {
                        SetHeaderIfAbsent(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        // Log message stuff
        public virtual string GetShortLogMessage(object payload) => $"headers={headers}{GetShortPayloadLogMessage(payload)}";

        public virtual string GetDetailedLogMessage(object payload) => $"headers={headers}{GetDetailedPayloadLogMessage(payload)}";

        public override string ToString() => $"{GetType().Name} [headers={headers}]";

        protected virtual MessageHeaderAccessor CreateMutableAccessor(IMessage message) => CreateMutableAccessor(message.Headers);

        protected virtual MessageHeaderAccessor CreateMutableAccessor(IMessageHeaders messageHeaders)
        {
            if (messageHeaders is not MessageHeaders asHeaders)
            {
                throw new InvalidOperationException("Unable to create mutable accessor, message has no headers or headers are not of type MessageHeaders");
            }

            return new MessageHeaderAccessor(asHeaders);
        }

        protected virtual bool IsReadOnly(string headerName) => Messaging.MessageHeaders.ID.Equals(headerName) || Messaging.MessageHeaders.TIMESTAMP.Equals(headerName);

        protected virtual void VerifyType(string headerName, object headerValue)
        {
            if (headerName != null && headerValue != null && (Messaging.MessageHeaders.ERROR_CHANNEL.Equals(headerName) ||
                        Messaging.MessageHeaders.REPLY_CHANNEL.EndsWith(headerName)) && !(headerValue is IMessageChannel || headerValue is string))
            {
                throw new ArgumentException(
                    $"'{headerName}' header value must be a MessageChannel or string");
            }
        }

        protected virtual string GetShortPayloadLogMessage(object payload)
        {
            switch (payload)
            {
                case string sPayload:
                    {
                        var payloadText = sPayload;
                        return payloadText.Length < 80
                            ? $" payload={payloadText}"
                            : $" payload={payloadText.Substring(0, 80)}...(truncated)";
                    }

                case byte[] bytes:
                    if (IsReadableContentType())
                    {
                        return bytes.Length < 80
                            ? $" payload={new string(Encoding.GetChars(bytes))}"
                            : $" payload={new string(Encoding.GetChars(bytes, 0, 80))}...(truncated)";
                    }
                    else
                    {
                        return $" payload=byte[{bytes.Length}]";
                    }

                default:
                    {
                        var payloadText = payload.ToString();
                        return payloadText.Length < 80
                            ? $" payload={payloadText}"
                            : $" payload={payload.GetType().Name}@{payload}";
                    }
            }
        }

        protected virtual string GetDetailedPayloadLogMessage(object payload)
        {
            if (payload is string)
            {
                return $" payload={payload}";
            }
            else if (payload is byte[] bytes)
            {
                if (IsReadableContentType())
                {
                    return $" payload={new string(Encoding.GetChars(bytes))}";
                }
                else
                {
                    return $" payload=byte[{bytes.Length}]";
                }
            }
            else
            {
                return $" payload={payload}";
            }
        }

        protected virtual bool IsReadableContentType()
        {
            var contentType = MimeType.ToMimeType(ContentType);
            foreach (var mimeType in READABLE_MIME_TYPES)
            {
                if (mimeType.Includes(contentType))
                {
                    return true;
                }
            }

            return false;
        }

        private List<string> GetMatchingHeaderNames(string pattern, IDictionary<string, object> headers)
        {
            if (headers == null)
            {
                return new List<string>();
            }

            var matchingHeaderNames = new List<string>();
            foreach (var key in headers.Keys)
            {
                if (PatternMatchUtils.SimpleMatch(pattern, key))
                {
                    matchingHeaderNames.Add(key);
                }
            }

            return matchingHeaderNames;
        }

        private Encoding Encoding
        {
            get
            {
                var contentType = MimeType.ToMimeType(ContentType);
                var charset = contentType?.Encoding;
                return charset ?? DEFAULT_CHARSET;
            }
        }

        protected class AccessorMessageHeaders : MessageHeaders
        {
            protected MessageHeaderAccessor accessor;
            private bool _mutable = true;

            public AccessorMessageHeaders(MessageHeaderAccessor accessor, IDictionary<string, object> headers)
                : base(headers, ID_VALUE_NONE, -1L)
            {
                this.accessor = accessor;
            }

            public AccessorMessageHeaders(MessageHeaderAccessor accessor, MessageHeaders other)
                : base(other)
            {
                this.accessor = accessor;
            }

            public new IDictionary<string, object> RawHeaders
            {
                get
                {
                    if (!_mutable)
                    {
                        throw new InvalidOperationException();
                    }

                    return base.RawHeaders;
                }
            }

            public virtual bool IsMutable => _mutable;

            public virtual void SetImmutable()
            {
                if (!_mutable)
                {
                    return;
                }

                if (Id == null)
                {
                    var idGenerator = accessor.IdGenerator ?? IdGenerator;
                    var id = idGenerator.GenerateId().ToString();
                    if (id != ID_VALUE_NONE)
                    {
                        RawHeaders[ID] = id;
                    }
                }

                if (Timestamp == null && accessor.EnableTimestamp)
                {
                    RawHeaders[TIMESTAMP] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }

                _mutable = false;
            }

            public virtual MessageHeaderAccessor Accessor => accessor;
        }
    }
}
