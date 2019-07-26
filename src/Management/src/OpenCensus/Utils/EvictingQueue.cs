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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    public class EvictingQueue<T> : IEnumerable<T>, IEnumerable
    {
        private readonly Queue<T> _delegate;
        private readonly int _maxSize;

        public int Count
        {
            get
            {
                return _delegate.Count;
            }
        }

        public EvictingQueue(int maxSize)
        {
            if (maxSize < 0)
            {
                throw new ArgumentOutOfRangeException("maxSize must be >= 0");
            }

            _maxSize = maxSize;
            _delegate = new Queue<T>(maxSize);
        }

        public int RemainingCapacity()
        {
            return _maxSize - _delegate.Count;
        }

        public bool Offer(T e)
        {
            return Add(e);
        }

        public bool Add(T e)
        {
            if (e == null)
            {
                throw new ArgumentNullException();
            }

            if (_maxSize == 0)
            {
                return true;
            }

            if (_delegate.Count == _maxSize)
            {
                _delegate.Dequeue();
            }

            _delegate.Enqueue(e);
            return true;
        }

        public bool AddAll(ICollection<T> collection)
        {
            foreach (var e in collection)
            {
                Add(e);
            }

            return true;
        }

        public bool Contains(T e)
        {
            if (e == null)
            {
                throw new ArgumentNullException();
            }

            return _delegate.Contains(e);
        }

        public T[] ToArray()
        {
            return _delegate.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _delegate.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _delegate.GetEnumerator();
        }
    }
}
