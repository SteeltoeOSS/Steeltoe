// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

public interface IHystrixCollapserOptions
{
    IHystrixCollapserKey CollapserKey { get; set; }

    RequestCollapserScope Scope { get; set; }

    bool RequestCacheEnabled { get; set; }

    int MaxRequestsInBatch { get; set; }

    int TimerDelayInMilliseconds { get; set; }

    int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    int MetricsRollingStatisticalWindowBuckets { get; set; }

    bool MetricsRollingPercentileEnabled { get; set; }

    int MetricsRollingPercentileWindowInMilliseconds { get; set; }

    int MetricsRollingPercentileWindowBuckets { get; set; }

    int MetricsRollingPercentileBucketSize { get; set; }
}