// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Util;

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
    public const string ContentTypeDotNetSerializedObject = "application/x-dotnet-serialized-object";

    public const string TypeId = "__TypeId__";
    public const string ContentTypeId = "__ContentTypeId__";
    public const string KeyTypeId = "__KeyTypeId__";
    private static readonly IIdGenerator DefaultIdGenerator = new DefaultIdGenerator();

    public static readonly string IdValueNone = string.Empty;
    private static volatile IIdGenerator _idGenerator;

    protected readonly IDictionary<string, object> Headers;

    internal static IIdGenerator IdGenerator
    {
        get => _idGenerator ?? DefaultIdGenerator;
        set => _idGenerator = value;
    }

    ICollection IDictionary.Keys => (ICollection)Keys;

    ICollection IDictionary.Values => (ICollection)Values;

    bool IDictionary.IsFixedSize => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => throw new NotImplementedException();

    protected internal virtual IDictionary<string, object> RawHeaders => Headers;

    public virtual string Id
    {
        get
        {
            if (!Headers.TryGetValue(IdName, out object result))
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
            if (!Headers.TryGetValue(TimestampName, out object result))
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
            Headers.TryGetValue(ReplyChannelName, out object chan);
            return chan;
        }
    }

    public virtual object ErrorChannel
    {
        get
        {
            Headers.TryGetValue(ErrorChannelName, out object chan);
            return chan;
        }
    }

    public virtual ICollection<string> Keys => Headers.Keys;

    public virtual ICollection<object> Values => Headers.Values;

    public virtual int Count => Headers.Count;

    public virtual bool IsReadOnly => true;

    public virtual bool IsSynchronized => false;

    public virtual object SyncRoot => throw new InvalidOperationException();

    public virtual bool IsFixedSize => true;

    object IDictionary.this[object key]
    {
        get => Get<object>((string)key);
        set => throw new InvalidOperationException();
    }

    public virtual object this[string key]
    {
        get => Get<object>(key);
        set => throw new InvalidOperationException();
    }

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
        set => throw new InvalidOperationException();
    }

    public MessageHeaders(IDictionary<string, object> headers = null)
        : this(headers, null, null)
    {
    }

    public MessageHeaders(IDictionary<string, object> headers, string id, long? timestamp)
    {
        Headers = headers != null ? new Dictionary<string, object>(headers) : new Dictionary<string, object>();
        UpdateHeaders(id, timestamp);
    }

    protected MessageHeaders(MessageHeaders other)
    {
        Headers = other.RawHeaders;
    }

    public static MessageHeaders From(MessageHeaders other)
    {
        return new MessageHeaders(other);
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
            return TryGetValue(asString, out object _);
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

    public virtual T Get<T>(string key)
    {
        if (TryGetValue(key, out object val))
        {
            return (T)val;
        }

        return default;
    }

    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return (IDictionaryEnumerator)Headers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Headers).GetEnumerator();
    }

    void IDictionary.Add(object key, object value)
    {
        Add((string)key, value);
    }

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

        foreach (KeyValuePair<string, object> pair in Headers)
        {
            if (!other.TryGetValue(pair.Key, out object otherValue))
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
}
