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

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicInteger
    {
        protected int _value;

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
