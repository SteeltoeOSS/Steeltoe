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

namespace Steeltoe.Common.Util
{
    public class AtomicLong
    {
        private long _value;

        public AtomicLong()
            : this(0)
        {
        }

        public AtomicLong(long value)
        {
            _value = value;
        }

        public long Value
        {
            get
            {
                return Interlocked.Read(ref _value);
            }

            set
            {
                Interlocked.Exchange(ref _value, value);
            }
        }

        public bool CompareAndSet(long expected, long update)
        {
            return Interlocked.CompareExchange(ref _value, update, expected) == expected;
        }

        public long GetAndSet(long value)
        {
            return Interlocked.Exchange(ref _value, value);
        }

        public long AddAndGet(long value)
        {
            return Interlocked.Add(ref _value, value);
        }
    }
}
