// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
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
