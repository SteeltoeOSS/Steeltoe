using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class ConcurrentIntrusiveList<T> where T : IElement<T>
    {
        private int size = 0;
        private T head = default(T);
        private object _lck = new object();

        public ConcurrentIntrusiveList() { }
        public void AddElement(T element)
        {
            lock (_lck)
            {
                if (element.Next != null || element.Previous != null || element.Equals(head))
                {
                    throw new ArgumentOutOfRangeException("Element already in a list");
                }

                size++;
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
            lock (_lck)
            {
                if (element.Next == null && element.Previous == null && !element.Equals(head))
                {
                    throw new ArgumentOutOfRangeException("Element not in the list");
                }
                size--;
                if (element.Previous == null)
                {
                    // This is the first element
                    head = element.Next;
                    if (head != null)
                    {
                        // If more than one element in the list.
                        head.Previous = default(T);
                        element.Next = default(T);
                    }
                }
                else if (element.Next == null)
                {
                    // This is the last element, and there is at least another element because
                    // element.getPrev() != null.
                    element.Previous.Next = default(T);
                    element.Previous = default(T);
                }
                else
                {
                    element.Previous.Next = element.Next;
                    element.Next.Previous = element.Previous;
                    element.Next = default(T);
                    element.Previous = default(T);
                }
            }
        }
        public int Count
        {
            get
            {
                return size;
            }
        }

        public IList<T> Copy()
        {
            lock (_lck)
            {
                List<T> all = new List<T>(size);
                for (T e = head; e != null; e = e.Next)
                {
                    all.Add(e);
                }
                return all;
            }
        }
    }
}

