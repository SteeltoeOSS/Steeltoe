using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
            foreach(var e in collection)
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
