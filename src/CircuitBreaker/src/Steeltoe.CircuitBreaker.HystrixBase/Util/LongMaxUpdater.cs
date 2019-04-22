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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class LongMaxUpdater
    {
        private readonly AtomicLong _value = new AtomicLong(long.MinValue);

        public void Update(long value)
        {
            while (true)
            {
                long current = _value.Value;
                if (current >= value)
                {
                    return;
                }

                if (_value.CompareAndSet(current, value))
                {
                    return;
                }
            }
        }

        public long Max
        {
            get { return _value.Value;  }
        }

        public void Reset()
        {
            _value.GetAndSet(long.MinValue);
        }

        public long MaxThenReset()
        {
            return _value.GetAndSet(long.MinValue);
        }

        public override string ToString()
        {
            return _value.Value.ToString();
        }
    }
}
