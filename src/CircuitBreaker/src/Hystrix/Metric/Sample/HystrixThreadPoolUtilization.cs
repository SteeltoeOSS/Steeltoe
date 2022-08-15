// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;

public class HystrixThreadPoolUtilization
{
    public int CurrentActiveCount { get; }

    public int CurrentCorePoolSize { get; }

    public int CurrentPoolSize { get; }

    public int CurrentQueueSize { get; }

    public HystrixThreadPoolUtilization(int currentActiveCount, int currentCorePoolSize, int currentPoolSize, int currentQueueSize)
    {
        CurrentActiveCount = currentActiveCount;
        CurrentCorePoolSize = currentCorePoolSize;
        CurrentPoolSize = currentPoolSize;
        CurrentQueueSize = currentQueueSize;
    }

    public static HystrixThreadPoolUtilization Sample(HystrixThreadPoolMetrics threadPoolMetrics)
    {
        return new HystrixThreadPoolUtilization(threadPoolMetrics.CurrentActiveCount, threadPoolMetrics.CurrentCorePoolSize, threadPoolMetrics.CurrentPoolSize,
            threadPoolMetrics.CurrentQueueSize);
    }
}
