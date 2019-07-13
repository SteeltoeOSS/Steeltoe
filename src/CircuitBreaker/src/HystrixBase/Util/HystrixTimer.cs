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
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class HystrixTimer
    {
        private static readonly HystrixTimer Instance = new HystrixTimer();

        private readonly List<TimerReference> timerList = new List<TimerReference>();
        private readonly object _lock = new object();

        private HystrixTimer()
        {
        }

        public static HystrixTimer GetInstance()
        {
            return Instance;
        }

        public static void Reset()
        {
            HystrixTimer timer = GetInstance();
            lock (timer._lock)
            {
                foreach (TimerReference refr in timer.timerList)
                {
                    refr.Dispose();
                }
            }
        }

        public TimerReference AddTimerListener(ITimerListener listener)
        {
            TimerReference refr = new TimerReference(listener, TimeSpan.FromMilliseconds(listener.IntervalTimeInMilliseconds));
            refr.Start();

            lock (_lock)
            {
                timerList.Add(refr);
            }

            return refr;
        }
    }
}
