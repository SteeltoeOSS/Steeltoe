// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Support
{
    public class MutableMessageHeaders : MessageHeaders
    {
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
            headers.Add(key, value);
        }

        public override void Add(KeyValuePair<string, object> item)
        {
            headers.Add(item);
        }

        public virtual void AddRange(IDictionary<string, object> map)
        {
            foreach (var entry in map)
            {
                headers.Add(entry);
            }
        }

        public override object this[string key] { get => headers[key]; set => headers[key] = value; }

        public override void Clear()
        {
            headers.Clear();
        }

        public override bool Remove(KeyValuePair<string, object> item)
        {
            return headers.Remove(item);
        }

        public override bool Remove(string key)
        {
            return headers.Remove(key);
        }

        private static string ExtractId(IDictionary<string, object> headers)
        {
            if (headers != null && headers.ContainsKey(ID))
            {
                var id = headers[ID];
                if (id is string)
                {
                    return id as string;
                }
                else if (id is byte[])
                {
                    return new Guid((byte[])id).ToString();
                }
                else if (id is Guid)
                {
                    return ((Guid)id).ToString();
                }
            }

            return null;
        }

        private static long? ExtractTimestamp(IDictionary<string, object> headers)
        {
            if (headers != null && headers.ContainsKey(TIMESTAMP))
            {
                var timestamp = headers[TIMESTAMP];
                return (timestamp is string) ? long.Parse((string)timestamp) : (long)timestamp;
            }

            return null;
        }
    }
}
