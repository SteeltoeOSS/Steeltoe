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

using Steeltoe.CircuitBreaker.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class LongAdder
    {
        private AtomicLong _value = new AtomicLong(0);

        public void Increment()
        {
            Add(1);
        }

        public void Decrement()
        {
            Add(-1);
        }

        public void Add(long value)
        {
            _value.AddAndGet(value);
        }

        public long Sum()
        {
            return _value.Value;
        }

        public void Reset()
        {
            _value.GetAndSet(0);
        }

        public long SumThenReset()
        {
            return _value.GetAndSet(0);
        }

        public override string ToString()
        {
            return _value.Value.ToString();
        }
    }
}
