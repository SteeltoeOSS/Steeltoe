// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Support;

public class MessageHeaderAccessor : IMessageHeaderAccessor
{
    private static readonly MimeType[] ReadableMimeTypes =
    {
        MimeTypeUtils.ApplicationJson,
        MimeTypeUtils.ApplicationXml,
        new("text", "*"),
        new("application", "*+json"),
        new("application", "*+xml")
    };

    public static readonly Encoding DefaultCharset = Encoding.UTF8;

    private bool _leaveMutable;

    protected AccessorMessageHeaders headers;

    private Encoding Encoding
    {
        get
        {
            var contentType = MimeType.ToMimeType(ContentType);
            Encoding charset = contentType?.Encoding;
            return charset ?? DefaultCharset;
        }
    }

    public virtual bool EnableTimestamp { get; set; }

    public virtual IIdGenerator IdGenerator { get; set; }

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
            if (GetHeader(Messaging.MessageHeaders.IdName) == null)
            {
                return null;
            }

            return GetHeader(Messaging.MessageHeaders.IdName).ToString();
        }
    }

    public virtual long? Timestamp
    {
        get
        {
            object value = GetHeader(Messaging.MessageHeaders.TimestampName);

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
            object value = GetHeader(Messaging.MessageHeaders.ContentType);

            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }

        set => SetHeader(Messaging.MessageHeaders.ContentType, value);
    }

    public virtual string ReplyChannelName
    {
        get => GetHeader(Messaging.MessageHeaders.ReplyChannelName) as string;
        set => SetHeader(Messaging.MessageHeaders.ReplyChannelName, value);
    }

    public virtual object ReplyChannel
    {
        get => GetHeader(Messaging.MessageHeaders.ReplyChannelName);
        set => SetHeader(Messaging.MessageHeaders.ReplyChannelName, value);
    }

    public virtual string ErrorChannelName
    {
        get => GetHeader(Messaging.MessageHeaders.ErrorChannelName) as string;
        set => SetHeader(Messaging.MessageHeaders.ErrorChannelName, value);
    }

    public virtual object ErrorChannel
    {
        get => GetHeader(Messaging.MessageHeaders.ErrorChannelName);
        set => SetHeader(Messaging.MessageHeaders.ErrorChannelName, value);
    }

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
    {
        return GetAccessor(message.Headers, accessorType);
    }

    public static T GetAccessor<T>(IMessageHeaders messageHeaders)
        where T : MessageHeaderAccessor
    {
        return (T)GetAccessor(messageHeaders, typeof(T));
    }

    public static MessageHeaderAccessor GetAccessor(IMessageHeaders messageHeaders, Type accessorType)
    {
        if (messageHeaders is AccessorMessageHeaders accessorMessageHeaders)
        {
            MessageHeaderAccessor headerAccessor = accessorMessageHeaders.Accessor;

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
    {
        return GetMutableAccessor(message.Headers, accessorType);
    }

    public static MessageHeaderAccessor GetMutableAccessor(IMessageHeaders headers, Type accessorType = null)
    {
        MessageHeaderAccessor messageHeaderAccessor = null;

        if (headers is AccessorMessageHeaders accessorMessageHeaders)
        {
            MessageHeaderAccessor headerAccessor = accessorMessageHeaders.Accessor;

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

    public virtual void SetImmutable()
    {
        headers.SetImmutable();
    }

    public virtual IMessageHeaders ToMessageHeaders()
    {
        return new MessageHeaders(headers);
    }

    public virtual IDictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>(headers);
    }

    // Generic header accessors
    public virtual object GetHeader(string headerName)
    {
        return headers.TryGetValue(headerName, out object value) ? value : null;
    }

    public virtual void SetHeader(string name, object value)
    {
        if (IsReadOnly(name))
        {
            throw new ArgumentException($"'{name}' header is read-only.", nameof(name));
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

        foreach (string pattern in headerPatterns)
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

        foreach (string headerToRemove in headersToRemove)
        {
            RemoveHeader(headerToRemove);
        }
    }

    public virtual void CopyHeaders(IDictionary<string, object> headersToCopy)
    {
        if (headersToCopy != null)
        {
            foreach (KeyValuePair<string, object> kvp in headersToCopy)
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
            foreach (KeyValuePair<string, object> kvp in headersToCopy)
            {
                if (!IsReadOnly(kvp.Key))
                {
                    SetHeaderIfAbsent(kvp.Key, kvp.Value);
                }
            }
        }
    }

    // Log message stuff
    public virtual string GetShortLogMessage(object payload)
    {
        return $"headers={headers}{GetShortPayloadLogMessage(payload)}";
    }

    public virtual string GetDetailedLogMessage(object payload)
    {
        return $"headers={headers}{GetDetailedPayloadLogMessage(payload)}";
    }

    public override string ToString()
    {
        return $"{GetType().Name} [headers={headers}]";
    }

    protected virtual MessageHeaderAccessor CreateMutableAccessor(IMessage message)
    {
        return CreateMutableAccessor(message.Headers);
    }

    protected virtual MessageHeaderAccessor CreateMutableAccessor(IMessageHeaders messageHeaders)
    {
        if (messageHeaders is not MessageHeaders asHeaders)
        {
            throw new InvalidOperationException(
                $"Unable to create mutable accessor, message has no headers or headers are not of type {nameof(MessageHeaders)}.");
        }

        return new MessageHeaderAccessor(asHeaders);
    }

    protected virtual bool IsReadOnly(string headerName)
    {
        return Messaging.MessageHeaders.IdName.Equals(headerName) || Messaging.MessageHeaders.TimestampName.Equals(headerName);
    }

    protected virtual void VerifyType(string headerName, object headerValue)
    {
        if (headerName == null || headerValue == null)
        {
            return;
        }

        if (!Messaging.MessageHeaders.ErrorChannelName.Equals(headerName) &&
            !Messaging.MessageHeaders.ReplyChannelName.EndsWith(headerName, StringComparison.Ordinal))
        {
            return;
        }

        if (headerValue is not (IMessageChannel or string))
        {
            throw new ArgumentException($"'{headerName}' header value must be a {nameof(IMessageChannel)} or {nameof(String)}.", nameof(headerValue));
        }
    }

    protected virtual string GetShortPayloadLogMessage(object payload)
    {
        switch (payload)
        {
            case string sPayload:
            {
                string payloadText = sPayload;
                return payloadText.Length < 80 ? $" payload={payloadText}" : $" payload={payloadText.Substring(0, 80)}...(truncated)";
            }

            case byte[] bytes:
                if (IsReadableContentType())
                {
                    return bytes.Length < 80
                        ? $" payload={new string(Encoding.GetChars(bytes))}"
                        : $" payload={new string(Encoding.GetChars(bytes, 0, 80))}...(truncated)";
                }

                return $" payload=byte[{bytes.Length}]";

            default:
            {
                string payloadText = payload.ToString();
                return payloadText.Length < 80 ? $" payload={payloadText}" : $" payload={payload.GetType().Name}@{payload}";
            }
        }
    }

    protected virtual string GetDetailedPayloadLogMessage(object payload)
    {
        if (payload is string)
        {
            return $" payload={payload}";
        }

        if (payload is byte[] bytes)
        {
            if (IsReadableContentType())
            {
                return $" payload={new string(Encoding.GetChars(bytes))}";
            }

            return $" payload=byte[{bytes.Length}]";
        }

        return $" payload={payload}";
    }

    protected virtual bool IsReadableContentType()
    {
        var contentType = MimeType.ToMimeType(ContentType);

        foreach (MimeType mimeType in ReadableMimeTypes)
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

        foreach (string key in headers.Keys)
        {
            if (PatternMatchUtils.SimpleMatch(pattern, key))
            {
                matchingHeaderNames.Add(key);
            }
        }

        return matchingHeaderNames;
    }

    protected class AccessorMessageHeaders : MessageHeaders
    {
        private bool _mutable = true;
        protected MessageHeaderAccessor accessor;

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

        public virtual MessageHeaderAccessor Accessor => accessor;

        public AccessorMessageHeaders(MessageHeaderAccessor accessor, IDictionary<string, object> headers)
            : base(headers, IdValueNone, -1L)
        {
            this.accessor = accessor;
        }

        public AccessorMessageHeaders(MessageHeaderAccessor accessor, MessageHeaders other)
            : base(other)
        {
            this.accessor = accessor;
        }

        public virtual void SetImmutable()
        {
            if (!_mutable)
            {
                return;
            }

            if (Id == null)
            {
                IIdGenerator idGenerator = accessor.IdGenerator ?? IdGenerator;
                string id = idGenerator.GenerateId();

                if (id != IdValueNone)
                {
                    RawHeaders[IdName] = id;
                }
            }

            if (Timestamp == null && accessor.EnableTimestamp)
            {
                RawHeaders[TimestampName] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            _mutable = false;
        }
    }
}
