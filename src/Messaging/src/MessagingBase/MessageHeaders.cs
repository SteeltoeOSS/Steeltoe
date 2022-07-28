// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Messaging;

public class MessageHeaders : IMessageHeaders
{
    public const string Internal = "internal_";

    public const string IdName = "id";
    public const string TimestampName = "timestamp";
    public const string ContentType = "contentType";
    public const string ReplyChannelName = "replyChannel";
    public const string ErrorChannelName = "errorChannel";
    public const string InferredArgumentType = $"{Internal}InferredArgumentType";

    public const string ContentTypeJson = "application/json";
    public const string ContentTypeTextPlain = "text/plain";
    public const string ContentTypeBytes = "application/octet-stream";
    public const string ContentTypeJsonAlt = "text/x-json";
    public const string ContentTypeXml = "application/xml";
    public const string ContentTypeJavaSerializedObject = "application/x-java-serialized-object";
    public const string ContentTypeDotnetSerializedObject = "application/x-dotnet-serialized-object";

    public const string TypeId = "__TypeId__";
    public const string ContentTypeId = "__ContentTypeId__";
    public const string KeyTypeId = "__KeyTypeId__";

    public static readonly string IdValueNone = string.Empty;

    protected readonly IDictionary<string, object> Headers;
    private static readonly IIdGenerator DefaultIdGenerator = new DefaultIdGenerator();
    private static volatile IIdGenerator _idGenerator;

    public MessageHeaders(IDictionary<string, object> headers = null)
        : this(headers, null, null)
    {
    }

    public MessageHeaders(IDictionary<string, object> headers, string id, long? timestamp)
    {
        this.Headers = headers != null ? new Dictionary<string, object>(headers) : new Dictionary<string, object>();
        UpdateHeaders(id, timestamp);
    }

    public static MessageHeaders From(MessageHeaders other)
    {
        return new MessageHeaders(other);
    }

    protected MessageHeaders(MessageHeaders other)
    {
        Headers = other.RawHeaders;
    }

    public virtual string Id
    {
        get
        {
            if (!Headers.TryGetValue(IdName, out var result))
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
            if (!Headers.TryGetValue(TimestampName, out var result))
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
            Headers.TryGetValue(ReplyChannelName, out var chan);
            return chan;
        }
    }

    public virtual object ErrorChannel
    {
        get
        {
            Headers.TryGetValue(ErrorChannelName, out var chan);
            return chan;
        }
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not MessageHeaders other)
        {
            return false;
        }

        return ContentsEqual(other.Headers);
    }

    public override int GetHashCode()
    {
        return Headers.GetHashCode();
    }

    public override string ToString()
    {
        return Headers.ToString();
    }

    public virtual bool ContainsKey(string key)
    {
        return Headers.ContainsKey(key);
    }

    public virtual bool TryGetValue(string key, out object value)
    {
        return Headers.TryGetValue(key, out value);
    }

    public virtual void Add(string key, object value)
    {
        Headers.Add(key, value);
    }

    public virtual void Add(object key, object value)
    {
        throw new InvalidOperationException();
    }

    public virtual void Add(KeyValuePair<string, object> item)
    {
        Headers.Add(item);
    }

    public virtual void Clear()
    {
        Headers.Clear();
    }

    public virtual bool Contains(KeyValuePair<string, object> item)
    {
        return Headers.Contains(item);
    }

    public virtual bool Contains(object key)
    {
        if (key is string asString)
        {
            return TryGetValue(asString, out var _);
        }

        return false;
    }

    public virtual void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        Headers.CopyTo(array, arrayIndex);
    }

    public virtual void CopyTo(Array array, int index)
    {
        throw new InvalidOperationException();
    }

    public virtual void Remove(object key)
    {
        throw new InvalidOperationException();
    }

    public virtual bool Remove(string key)
    {
        return Headers.Remove(key);
    }

    public virtual bool Remove(KeyValuePair<string, object> item)
    {
        return Headers.Remove(item);
    }

    public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return Headers.GetEnumerator();
    }

    public virtual ICollection<string> Keys => Headers.Keys;

    public virtual ICollection<object> Values => Headers.Values;

    public virtual int Count => Headers.Count;

    public virtual bool IsReadOnly => true;

    public virtual object this[string key] { get => Get<object>(key); set => throw new InvalidOperationException(); }

    public virtual object this[object key]
    {
        get
        {
            if (key is string asString)
            {
                return Get<object>(asString);
            }

            return null;
        }

        set
        {
            throw new InvalidOperationException();
        }
    }

    public virtual T Get<T>(string key)
    {
        if (TryGetValue(key, out var val))
        {
            return (T)val;
        }

        return default;
    }

    public virtual bool IsSynchronized => false;

    public virtual object SyncRoot => throw new InvalidOperationException();

    public virtual bool IsFixedSize => true;

    ICollection IDictionary.Keys => (ICollection)Keys;

    ICollection IDictionary.Values => (ICollection)Values;

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return (IDictionaryEnumerator)Headers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Headers).GetEnumerator();
    }

    void IDictionary.Add(object key, object value) => Add((string)key, value);

    bool IDictionary.Contains(object key)
    {
        return Headers.ContainsKey((string)key);
    }

    void IDictionary.Remove(object key)
    {
        Headers.Remove((string)key);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        var collection = new KeyValuePair<string, object>[array.Length];
        Headers.CopyTo(collection, index);
        collection.CopyTo(array, 0);
    }

    bool IDictionary.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => throw new NotImplementedException();

    object IDictionary.this[object key] { get => Get<object>((string)key); set => throw new InvalidOperationException(); }

    private bool ContentsEqual(IDictionary<string, object> other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (Headers.Count != other.Count)
        {
            return false;
        }

        foreach (var pair in Headers)
        {
            if (!other.TryGetValue(pair.Key, out var otherValue))
            {
                return false;
            }

            if (!Equals(pair.Value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    internal static IIdGenerator IdGenerator
    {
        get
        {
            return _idGenerator ?? DefaultIdGenerator;
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
            Headers[IdName] = IdGenerator.GenerateId();
        }
        else if (id == IdValueNone)
        {
            Headers.Remove(IdName);
        }
        else
        {
            Headers[IdName] = id;
        }

        if (timestamp == null)
        {
            Headers[TimestampName] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        else if (timestamp.Value < 0)
        {
            Headers.Remove(TimestampName);
        }
        else
        {
            Headers[TimestampName] = timestamp.Value;
        }
    }

    protected internal virtual IDictionary<string, object> RawHeaders
    {
        get { return Headers; }
    }
}
