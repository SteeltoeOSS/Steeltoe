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

namespace Steeltoe.CircuitBreaker.Util
{
    public class AtomicBoolean
    {
        private volatile int _value;

        public AtomicBoolean()
            : this(false)
        {
        }

        public AtomicBoolean(bool value)
        {
            Value = value;
        }

        public bool Value
        {
            get => _value != 0;

            set => _value = value ? 1 : 0;
        }

        public bool CompareAndSet(bool expected, bool update)
        {
            var expectedInt = expected ? 1 : 0;
            var updateInt = update ? 1 : 0;
            return Interlocked.CompareExchange(ref _value, updateInt, expectedInt) == expectedInt;
        }
    }
}
