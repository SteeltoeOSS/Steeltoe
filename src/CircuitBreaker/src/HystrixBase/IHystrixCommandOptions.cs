// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

public enum ExecutionIsolationStrategy
{
    THREAD,
    SEMAPHORE
}

public interface IHystrixCommandOptions
{
    IHystrixCommandGroupKey GroupKey { get; set; }

    IHystrixCommandKey CommandKey { get; set; }

    IHystrixThreadPoolKey ThreadPoolKey { get; set; }

    bool CircuitBreakerEnabled { get; set; }

    int CircuitBreakerErrorThresholdPercentage { get; set; }

    bool CircuitBreakerForceClosed { get; set; }

    bool CircuitBreakerForceOpen { get; set; }

    int CircuitBreakerRequestVolumeThreshold { get; set; }

    int CircuitBreakerSleepWindowInMilliseconds { get; set; }

    int ExecutionIsolationSemaphoreMaxConcurrentRequests { get; set; }

    ExecutionIsolationStrategy ExecutionIsolationStrategy { get; set; }

    string ExecutionIsolationThreadPoolKeyOverride { get; set; }

    int ExecutionTimeoutInMilliseconds { get; set; }

    bool ExecutionTimeoutEnabled { get; set; }

    int FallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }

    bool FallbackEnabled { get; set; }

    int MetricsHealthSnapshotIntervalInMilliseconds { get; set; }

    int MetricsRollingPercentileBucketSize { get; set; }

    bool MetricsRollingPercentileEnabled { get; set; }

    int MetricsRollingPercentileWindow { get; set; }

    int MetricsRollingPercentileWindowInMilliseconds { get; set; }

    int MetricsRollingPercentileWindowBuckets { get; set; }

    int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    int MetricsRollingStatisticalWindowBuckets { get; set; }

    bool RequestCacheEnabled { get; set; }

    bool RequestLogEnabled { get; set; }

    IHystrixThreadPoolOptions ThreadPoolOptions { get; set; }
}
