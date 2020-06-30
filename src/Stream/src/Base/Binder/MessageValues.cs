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

using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Steeltoe.Stream.Binder
{
    public class MessageValues : IDictionary<string, object>
    {
        public MessageValues(IMessage message)
        {
            Payload = message.Payload;
            Headers = new Dictionary<string, object>();
            foreach (var header in message.Headers)
            {
                Headers.Add(header.Key, header.Value);
            }
        }

        public MessageValues(object payload, IDictionary<string, object> headers)
        {
            Payload = payload;
            Headers = new Dictionary<string, object>(headers);
        }

        public object Payload { get; set; }

        public Dictionary<string, object> Headers { get; set; }

        public IMessage ToMessage()
        {
            return IntegrationMessageBuilder.WithPayload(Payload).CopyHeaders(Headers).Build();
        }

        public void CopyHeadersIfAbsent(IDictionary<string, object> headersToCopy)
        {
            foreach (var headersToCopyEntry in headersToCopy)
            {
                if (!ContainsKey(headersToCopyEntry.Key))
                {
                    Add(headersToCopyEntry.Key, headersToCopyEntry.Value);
                }
            }
        }

        public object this[string key]
        {
            get
            {
                Headers.TryGetValue(key, out var result);
                return result;
            }
            set => Headers[key] = value;
        }

        public ICollection<string> Keys => Headers.Keys;

        public ICollection<object> Values => Headers.Values;

        public int Count => Headers.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            Headers.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Headers.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Headers.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Headers.ContainsKey(item.Key) && Headers.ContainsValue(item.Value);
        }

        public bool ContainsKey(string key)
        {
            return Headers.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)Headers).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Headers.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return Headers.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Headers.Remove(item.Key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            return Headers.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Headers.GetEnumerator();
        }
    }
}
