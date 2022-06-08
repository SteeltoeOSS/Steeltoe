// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Steeltoe.Common.Util;

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
        var ticksToWait = maxWaitMilli * TimeSpan.TicksPerMillisecond;
        var start = DateTime.Now.Ticks;

        while (true)
        {
            var elapsed = DateTime.Now.Ticks - start;
            var ticksLeft = ticksToWait - elapsed;

            if (check())
            {
                return true;
            }

            if (ticksToWait <= 0)
            {
                return false;
            }

            if (elapsed >= ticksToWait)
            {
                return false;
            }

            DoWait(ticksLeft);

            if (check())
            {
                return true;
            }
        }
    }

    // Used by unit tests only
    public static void Wait(int maxWaitMilli)
    {
        if (maxWaitMilli <= 0)
        {
            return;
        }

        Thread.Sleep(maxWaitMilli);

        // long ticksToWait = maxWaitMilli * TimeSpan.TicksPerMillisecond;

        // if (ticksToWait <= 0)
        // {
        //    return;
        // }

        // long start = DateTime.Now.Ticks;

        // while (true)
        // {
        //    long elapsed = DateTime.Now.Ticks - start;
        //    long ticksLeft = ticksToWait - elapsed;

        // if (elapsed >= ticksToWait)
        //    {
        //        return;
        //    }

        // DoWait(ticksLeft);
        // }
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
