// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        public MutableMessageHeaders(IDictionary<string, object> headers, Guid? id, long? timestamp)
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

        private static Guid? ExtractId(IDictionary<string, object> headers)
        {
            if (headers != null && headers.ContainsKey(ID))
            {
                var id = headers[ID];
                if (id is string)
                {
                    return Guid.Parse((string)id);
                }
                else if (id is byte[])
                {
                    return new Guid((byte[])id);
                }
                else
                {
                    return (Guid)id;
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
