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
        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long CurrentTimeMillis
        {
            get
            {
                return DateTime.Now.Ticks / 10000;
            }
        }

        public static long CurrentTimeMillisJava
        {
            get
            {
                long javaTicks = DateTime.Now.Ticks - baseTime.Ticks;
                return javaTicks / 10000;
            }
        }

        public static bool WaitUntil(Func<bool> check, int maxWaitMilli)
        {
            SpinWait sw = default(SpinWait);

            long start = DateTime.Now.Ticks;
            long ticksToWait = maxWaitMilli * 10000;

            while (!check())
            {
                long elapsed = DateTime.Now.Ticks - start;
                if (elapsed >= ticksToWait)
                {
                    return false;
                }

                sw.SpinOnce();
            }

            return true;
        }

        public static void Wait(int maxWaitMilli)
        {
            WaitUntil(() => { return false; }, maxWaitMilli);
        }
    }
}
