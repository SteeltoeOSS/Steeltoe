// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IHystrixThreadPoolOptions
{
    IHystrixThreadPoolKey ThreadPoolKey { get; }

    int CoreSize { get; set; }

    int MaximumSize { get; set; }

    int KeepAliveTimeMinutes { get; set; }

    int MaxQueueSize { get; set; }

    int QueueSizeRejectionThreshold { get; set; }

    bool AllowMaximumSizeToDivergeFromCoreSize { get; set; }

    int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    int MetricsRollingStatisticalWindowBuckets { get; set; }
}