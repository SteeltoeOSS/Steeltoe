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

        public override void Add(string key, object value) => headers.Add(key, value);

        public override void Add(KeyValuePair<string, object> item) => headers.Add(item);

        public virtual void AddRange(IDictionary<string, object> map)
        {
            foreach (var entry in map)
            {
                headers.Add(entry);
            }
        }

        public override object this[string key] { get => headers[key]; set => headers[key] = value; }

        public override void Clear() => headers.Clear();

        public override bool Remove(KeyValuePair<string, object> item) => headers.Remove(item);

        public override bool Remove(string key) => headers.Remove(key);

        private static string ExtractId(IDictionary<string, object> headers)
        {
            if (headers != null && headers.ContainsKey(ID))
            {
                var id = headers[ID];
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
            if (headers != null && headers.ContainsKey(TIMESTAMP))
            {
                var timestamp = headers[TIMESTAMP];
                return (timestamp is string strTimestamp) ? long.Parse(strTimestamp) : (long)timestamp;
            }

            return null;
        }
    }
}
