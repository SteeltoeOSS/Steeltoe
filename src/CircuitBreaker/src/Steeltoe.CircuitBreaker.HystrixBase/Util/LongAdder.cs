// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
