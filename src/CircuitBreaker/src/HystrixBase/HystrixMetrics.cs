// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class HystrixMetrics
{
    private readonly HystrixRollingNumber _counter;

    protected HystrixMetrics(HystrixRollingNumber counter)
    {
        _counter = counter;
    }

    public virtual long GetCumulativeCount(HystrixRollingNumberEvent @event)
    {
        return _counter.GetCumulativeSum(@event);
    }

    public virtual long GetRollingCount(HystrixRollingNumberEvent @event)
    {
        return _counter.GetRollingSum(@event);
    }
}
