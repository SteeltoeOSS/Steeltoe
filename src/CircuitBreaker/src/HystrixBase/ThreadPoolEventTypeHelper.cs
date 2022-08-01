// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class ThreadPoolEventTypeHelper
{
    public static IList<ThreadPoolEventType> Values { get; } = new List<ThreadPoolEventType>();

    static ThreadPoolEventTypeHelper()
    {
        Values.Add(ThreadPoolEventType.Executed);
        Values.Add(ThreadPoolEventType.Rejected);
    }

    public static ThreadPoolEventType From(this HystrixRollingNumberEvent @event)
    {
        return @event switch
        {
            HystrixRollingNumberEvent.ThreadExecution => ThreadPoolEventType.Executed,
            HystrixRollingNumberEvent.ThreadPoolRejected => ThreadPoolEventType.Rejected,
            _ => throw new ArgumentOutOfRangeException($"Not an event that can be converted to HystrixEventType.ThreadPool : {@event}"),
        };
    }

    public static ThreadPoolEventType From(this HystrixEventType eventType)
    {
        return eventType switch
        {
            HystrixEventType.Success => ThreadPoolEventType.Executed,
            HystrixEventType.Failure => ThreadPoolEventType.Executed,
            HystrixEventType.Timeout => ThreadPoolEventType.Executed,
            HystrixEventType.BadRequest => ThreadPoolEventType.Executed,
            HystrixEventType.ThreadPoolRejected => ThreadPoolEventType.Rejected,
            _ => ThreadPoolEventType.Unknown,
        };
    }
}
