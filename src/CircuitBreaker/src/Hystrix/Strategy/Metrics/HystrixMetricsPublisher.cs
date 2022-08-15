// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;

public abstract class HystrixMetricsPublisher
{
    public virtual IHystrixMetricsPublisherCommand GetMetricsPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandGroupKey,
        HystrixCommandMetrics metrics, ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
    {
        return new HystrixMetricsPublisherCommandDefault(commandKey, commandGroupKey, metrics, circuitBreaker, properties);
    }

    public virtual IHystrixMetricsPublisherThreadPool GetMetricsPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics,
        IHystrixThreadPoolOptions properties)
    {
        return new HystrixMetricsPublisherThreadPoolDefault(threadPoolKey, metrics, properties);
    }

    public virtual IHystrixMetricsPublisherCollapser GetMetricsPublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics,
        IHystrixCollapserOptions properties)
    {
        return new HystrixMetricsPublisherCollapserDefault(collapserKey, metrics, properties);
    }
}
