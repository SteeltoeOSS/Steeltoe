// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public class MutableMessageHeaders : MessageHeaders
{
    public override object this[string key]
    {
        get => Headers[key];
        set => Headers[key] = value;
    }

    public MutableMessageHeaders(IDictionary<string, object> headers)
        : base(headers, ExtractId(headers), ExtractTimestamp(headers))
    {
    }

    public MutableMessageHeaders(IDictionary<string, object> headers, string id, long? timestamp)
        : base(headers, id, timestamp)
    {
    }

    public override void Add(string key, object value)
    {
        Headers.Add(key, value);
    }

    public override void Add(KeyValuePair<string, object> item)
    {
        Headers.Add(item);
    }

    public virtual void AddRange(IDictionary<string, object> map)
    {
        foreach (KeyValuePair<string, object> entry in map)
        {
            Headers.Add(entry);
        }
    }

    public override void Clear()
    {
        Headers.Clear();
    }

    public override bool Remove(KeyValuePair<string, object> item)
    {
        return Headers.Remove(item);
    }

    public override bool Remove(string key)
    {
        return Headers.Remove(key);
    }

    private static string ExtractId(IDictionary<string, object> headers)
    {
        if (headers != null && headers.ContainsKey(IdName))
        {
            object id = headers[IdName];

            switch (id)
            {
                case string idAsString:
                    return idAsString;
                case byte[] v:
                    return new Guid(v).ToString();
                case Guid guid:
                    return guid.ToString();
            }
        }

        return null;
    }

    private static long? ExtractTimestamp(IDictionary<string, object> headers)
    {
        if (headers != null && headers.ContainsKey(TimestampName))
        {
            object timestamp = headers[TimestampName];
            return timestamp is string strTimestamp ? long.Parse(strTimestamp) : (long)timestamp;
        }

        return null;
    }
}
