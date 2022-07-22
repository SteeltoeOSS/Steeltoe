// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class ThreadPoolEventTypeHelper
{
    public static IList<ThreadPoolEventType> Values { get; } = new List<ThreadPoolEventType>();

    static ThreadPoolEventTypeHelper()
    {
        Values.Add(ThreadPoolEventType.EXECUTED);
        Values.Add(ThreadPoolEventType.REJECTED);
    }

    public static ThreadPoolEventType From(this HystrixRollingNumberEvent @event)
    {
        return @event switch
        {
            HystrixRollingNumberEvent.THREAD_EXECUTION => ThreadPoolEventType.EXECUTED,
            HystrixRollingNumberEvent.THREAD_POOL_REJECTED => ThreadPoolEventType.REJECTED,
            _ => throw new ArgumentOutOfRangeException("Not an event that can be converted to HystrixEventType.ThreadPool : " + @event),
        };
    }

    public static ThreadPoolEventType From(this HystrixEventType eventType)
    {
        return eventType switch
        {
            HystrixEventType.SUCCESS => ThreadPoolEventType.EXECUTED,
            HystrixEventType.FAILURE => ThreadPoolEventType.EXECUTED,
            HystrixEventType.TIMEOUT => ThreadPoolEventType.EXECUTED,
            HystrixEventType.BAD_REQUEST => ThreadPoolEventType.EXECUTED,
            HystrixEventType.THREAD_POOL_REJECTED => ThreadPoolEventType.REJECTED,
            _ => ThreadPoolEventType.UNKNOWN,
        };
    }
}