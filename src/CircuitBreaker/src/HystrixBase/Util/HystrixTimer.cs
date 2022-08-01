// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public class HystrixTimer
{
    private static readonly HystrixTimer Instance = new ();

    private readonly List<TimerReference> _timerList = new ();
    private readonly object _lock = new ();

    private HystrixTimer()
    {
    }

    public static HystrixTimer GetInstance()
    {
        return Instance;
    }

    public static void Reset()
    {
        var timer = GetInstance();
        lock (timer._lock)
        {
            foreach (var reference in timer._timerList)
            {
                reference.Dispose();
            }
        }
    }

    public TimerReference AddTimerListener(ITimerListener listener)
    {
        var reference = new TimerReference(listener, TimeSpan.FromMilliseconds(listener.IntervalTimeInMilliseconds));
        reference.Start();

        lock (_lock)
        {
            _timerList.Add(reference);
        }

        return reference;
    }
}
