// <copyright file="ConcurrentIntrusiveList.cs" company="OpenCensus Authors">
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
    using System.Collections.Generic;

    internal sealed class ConcurrentIntrusiveList<T> where T : IElement<T>
    {
        private readonly object lck = new object();
        private T head = default;

        public ConcurrentIntrusiveList()
        {
        }

        public int Count { get; private set; } = 0;

        public void AddElement(T element)
        {
            lock (lck)
            {
                if (element.Next != null || element.Previous != null || element.Equals(head))
                {
                    throw new ArgumentOutOfRangeException("Element already in a list");
                }

                Count++;
                if (head == null)
                {
                    head = element;
                }
                else
                {
                    head.Previous = element;
                    element.Next = head;
                    head = element;
                }
            }
        }

        public void RemoveElement(T element)
        {
            lock (lck)
            {
                if (element.Next == null && element.Previous == null && !element.Equals(head))
                {
                    throw new ArgumentOutOfRangeException("Element not in the list");
                }

                Count--;
                if (element.Previous == null)
                {
                    // This is the first element
                    head = element.Next;
                    if (head != null)
                    {
                        // If more than one element in the list.
                        head.Previous = default;
                        element.Next = default;
                    }
                }
                else if (element.Next == null)
                {
                    // This is the last element, and there is at least another element because
                    // element.getPrev() != null.
                    element.Previous.Next = default;
                    element.Previous = default;
                }
                else
                {
                    element.Previous.Next = element.Next;
                    element.Next.Previous = element.Previous;
                    element.Next = default;
                    element.Previous = default;
                }
            }
        }

        public IList<T> Copy()
        {
            lock (lck)
            {
                var all = new List<T>(Count);
                for (var e = head; e != null; e = e.Next)
                {
                    all.Add(e);
                }

                return all;
            }
        }
    }
}
