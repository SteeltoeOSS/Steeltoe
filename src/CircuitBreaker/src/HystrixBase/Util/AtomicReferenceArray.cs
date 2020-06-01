// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicReferenceArray<T>
    {
        private readonly T[] _array;

        public AtomicReferenceArray(int length)
        {
            this._array = new T[length];
        }

        public T this[int index]
        {
            get
            {
                lock (this._array)
                {
                    return this._array[index];
                }
            }

            set
            {
                lock (this._array)
                {
                    this._array[index] = value;
                }
            }
        }

        public T[] ToArray()
        {
            lock (this._array)
            {
                return (T[])this._array.Clone();
            }
        }

        public int Length
        {
            get
            {
                lock (this._array)
                {
                    return _array.Length;
                }
            }
        }
    }
}
