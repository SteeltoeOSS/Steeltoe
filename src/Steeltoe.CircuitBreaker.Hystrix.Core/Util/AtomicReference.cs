//
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

using System.Threading;


namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicReference<T> where T : class
    {
        private  T _value;

        public AtomicReference() : this((T) default(T))
        {
        }

        public AtomicReference(T value)
        {
            _value = value;
        }

        public T Value
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

        public bool CompareAndSet(T expected, T update)
        {
            return Interlocked.CompareExchange(ref this._value, update, expected) == expected;
        }
        public T GetAndSet(T value)
        {
            return Interlocked.Exchange(ref this._value, value);
        }
    }
}
