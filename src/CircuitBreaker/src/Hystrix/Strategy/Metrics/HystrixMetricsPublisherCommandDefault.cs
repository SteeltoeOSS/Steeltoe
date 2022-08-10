// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;

public class HystrixMetricsPublisherCommandDefault : IHystrixMetricsPublisherCommand
{
    public HystrixMetricsPublisherCommandDefault(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandGroupKey, HystrixCommandMetrics metrics,
        ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
    {
        // do nothing by default
    }

    public void Initialize()
    {
        // do nothing by default
    }
}
