// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class LongAdder
    {
        private AtomicLong _value = new AtomicLong(0);

        public void Increment()
        {
            this.Add(1);
        }

        public void Decrement()
        {
            this.Add(-1);
        }

        public void Add(long value)
        {
            this._value.AddAndGet(value);
        }

        public long Sum()
        {
            return this._value.Value;
        }

        public void Reset()
        {
            this._value.GetAndSet(0);
        }

        public long SumThenReset()
        {
            return this._value.GetAndSet(0);
        }

        public override string ToString()
        {
            return this._value.Value.ToString();
        }
    }
}
