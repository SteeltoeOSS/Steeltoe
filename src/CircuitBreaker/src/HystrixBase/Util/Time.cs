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

using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public static class Time
    {
        private const int SPIN_WAIT_ITERATIONS = 5;
        private const long YIELD_THRESHOLD = 1000;
        private const long SLEEP_THRESHOLD = TimeSpan.TicksPerMillisecond;

        public static long CurrentTimeMillis
        {
            get
            {
                return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }

        public static long CurrentTimeMillisJava
        {
            get
            {
                return DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        public static bool WaitUntil(Func<bool> check, int maxWaitMilli)
        {
            long ticksToWait = maxWaitMilli * TimeSpan.TicksPerMillisecond;
            long start = DateTime.Now.Ticks;

            while (true)
            {
                long elapsed = DateTime.Now.Ticks - start;
                long ticksLeft = ticksToWait - elapsed;

                if (check())
                {
                    return true;
                }

                if (elapsed >= ticksToWait)
                {
                    return false;
                }

                DoWait(ticksLeft);
            }
        }

        public static void Wait(int maxWaitMilli)
        {
            long ticksToWait = maxWaitMilli * TimeSpan.TicksPerMillisecond;
            long start = DateTime.Now.Ticks;

            while (true)
            {
                long elapsed = DateTime.Now.Ticks - start;
                long ticksLeft = ticksToWait - elapsed;

                if (elapsed >= ticksToWait)
                {
                    return;
                }

                DoWait(ticksLeft);
            }
        }

        private static void DoWait(long ticksLeft)
        {
            if (ticksLeft > SLEEP_THRESHOLD)
            {
                Thread.Sleep(1);
            }
            else if (ticksLeft > YIELD_THRESHOLD)
            {
                Thread.Yield();
            }
            else
            {
                Thread.SpinWait(SPIN_WAIT_ITERATIONS);
            }
        }
    }
}
