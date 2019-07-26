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

using Steeltoe.Management.Census.Common;
using System;

namespace Steeltoe.Management.Census.Testing.Common
{
    [Obsolete]
    public class TestClock : IClock
    {
        private const int NUM_NANOS_PER_SECOND = 1000 * 1000 * 1000;
        private readonly object _lck = new object();
        private ITimestamp currentTime = Timestamp.Create(1493419949, 223123456);

        public static TestClock Create()
        {
            return new TestClock();
        }

        public static TestClock Create(ITimestamp time)
        {
            return new TestClock
            {
                Time = time
            };
        }

        public ITimestamp Time
        {
            get
            {
                lock (_lck)
                {
                    return currentTime;
                }
            }

            set
            {
                lock (_lck)
                {
                    currentTime = value;
                }
            }
        }

        public void AdvanceTime(IDuration duration)
        {
            lock (_lck)
            {
                currentTime = currentTime.AddDuration(duration);
            }
        }

        public ITimestamp Now
        {
            get
            {
                lock (_lck)
                {
                    return currentTime;
                }
            }
        }

        public long NowNanos
        {
            get
            {
                lock (_lck)
                {
                    return GetNanos(currentTime);
                }
            }
        }

        private static long GetNanos(ITimestamp time)
        {
            var nanoSeconds = time.Seconds * NUM_NANOS_PER_SECOND;
            return nanoSeconds + time.Nanos;
        }

        private TestClock()
        {
        }
    }
}
