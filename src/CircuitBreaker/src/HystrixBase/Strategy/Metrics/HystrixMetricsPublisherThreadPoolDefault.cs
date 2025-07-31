// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class HystrixMetricsPublisherThreadPoolDefault : IHystrixMetricsPublisherThreadPool
{
    public HystrixMetricsPublisherThreadPoolDefault(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
    {
        // do nothing by default
    }

    public void Initialize()
    {
        // do nothing by default
    }
}