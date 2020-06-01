// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicInteger
    {
        protected volatile int _value;

        public AtomicInteger()
            : this(0)
        {
        }

        public AtomicInteger(int value)
        {
            _value = value;
        }

        public int Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }

        public bool CompareAndSet(int expected, int update)
        {
            return Interlocked.CompareExchange(ref this._value, update, expected) == expected;
        }

        public int IncrementAndGet()
        {
            return Interlocked.Increment(ref this._value);
        }

        public int DecrementAndGet()
        {
            return Interlocked.Decrement(ref this._value);
        }

        public int GetAndIncrement()
        {
            return Interlocked.Increment(ref this._value) - 1;
        }

        public int AddAndGet(int value)
        {
            return Interlocked.Add(ref this._value, value);
        }
    }
}
