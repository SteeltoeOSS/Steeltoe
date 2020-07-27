// <copyright file="EvictingQueue.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    internal class EvictingQueue<T> : IEnumerable<T>, IEnumerable
    {
        private readonly Queue<T> @delegate;
        private readonly int maxSize;

        public EvictingQueue(int maxSize)
        {
            if (maxSize < 0)
            {
                throw new ArgumentOutOfRangeException("maxSize must be >= 0");
            }

            this.maxSize = maxSize;
            @delegate = new Queue<T>(maxSize);
        }

        public int Count
        {
            get
            {
                return @delegate.Count;
            }
        }

        public int RemainingCapacity()
        {
            return maxSize - @delegate.Count;
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

            if (maxSize == 0)
            {
                return true;
            }

            if (@delegate.Count == maxSize)
            {
                @delegate.Dequeue();
            }

            @delegate.Enqueue(e);
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

            return @delegate.Contains(e);
        }

        public T[] ToArray()
        {
            return @delegate.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return @delegate.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return @delegate.GetEnumerator();
        }
    }
}
