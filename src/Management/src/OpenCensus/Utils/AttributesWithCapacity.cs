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

using Steeltoe.Management.Census.Trace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    internal class AttributesWithCapacity : IDictionary<string, IAttributeValue>
    {
        private OrderedDictionary _delegate = new OrderedDictionary();
        private readonly int capacity;
        private int totalRecordedAttributes;

        public int NumberOfDroppedAttributes
        {
            get
            {
                return totalRecordedAttributes - Count;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return (ICollection<string>)_delegate.Keys;
            }
        }

        public ICollection<IAttributeValue> Values
        {
            get
            {
                return (ICollection<IAttributeValue>)_delegate.Values;
            }
        }

        public int Count
        {
            get
            {
                return _delegate.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return _delegate.IsReadOnly;
            }
        }

        public IAttributeValue this[string key]
        {
            get
            {
                return (IAttributeValue)_delegate[key];
            }

            set
            {
                _delegate[key] = value;
            }
        }

        public AttributesWithCapacity(int capacity)
        {
            this.capacity = capacity;
        }

        public void PutAttribute(string key, IAttributeValue value)
        {
            totalRecordedAttributes += 1;
            this[key] = value;
            if (Count > capacity)
            {
                _delegate.RemoveAt(0);
            }
        }

        // Users must call this method instead of putAll to keep count of the total number of entries
        // inserted.
        public void PutAttributes(IDictionary<string, IAttributeValue> attributes)
        {
            foreach (var kvp in attributes)
            {
                PutAttribute(kvp.Key, kvp.Value);
            }
        }

        public void Add(string key, IAttributeValue value)
        {
            _delegate.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _delegate.Contains(key);
        }

        public bool Remove(string key)
        {
            if (_delegate.Contains(key))
            {
                _delegate.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(string key, out IAttributeValue value)
        {
            value = null;
            if (ContainsKey(key))
            {
                value = (IAttributeValue)_delegate[key];
                return true;
            }

            return false;
        }

        public void Add(KeyValuePair<string, IAttributeValue> item)
        {
            _delegate.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _delegate.Clear();
        }

        public bool Contains(KeyValuePair<string, IAttributeValue> item)
        {
            var result = TryGetValue(item.Key, out IAttributeValue value);
            if (result)
            {
                return value.Equals(item.Value);
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, IAttributeValue>[] array, int arrayIndex)
        {
            DictionaryEntry[] entries = new DictionaryEntry[_delegate.Count];
            _delegate.CopyTo(entries, 0);

            for (int i = 0; i < entries.Length; i++)
            {
                array[i + arrayIndex] = new KeyValuePair<string, IAttributeValue>((string)entries[i].Key, (IAttributeValue)entries[i].Value);
            }
        }

        public bool Remove(KeyValuePair<string, IAttributeValue> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, IAttributeValue>> GetEnumerator()
        {
            var array = new KeyValuePair<string, IAttributeValue>[_delegate.Count];
            CopyTo(array, 0);
            return array.ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _delegate.GetEnumerator();
        }
    }
}
