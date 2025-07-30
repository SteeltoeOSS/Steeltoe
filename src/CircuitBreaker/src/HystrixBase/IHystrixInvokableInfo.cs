// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IHystrixInvokableInfo
{
    IHystrixCommandGroupKey CommandGroup { get; }

    IHystrixCommandKey CommandKey { get; }

    IHystrixThreadPoolKey ThreadPoolKey { get; }

    string PublicCacheKey { get; }

    IHystrixCollapserKey OriginatingCollapserKey { get; }

    HystrixCommandMetrics Metrics { get; }

    IHystrixCommandOptions CommandOptions { get; }

    bool IsCircuitBreakerOpen { get; }

    bool IsExecutionComplete { get; }

    bool IsExecutedInThread { get; }

    bool IsSuccessfulExecution { get; }

    bool IsFailedExecution { get; }

    Exception FailedExecutionException { get; }

    bool IsResponseFromFallback { get; }

    bool IsResponseTimedOut { get; }

    bool IsResponseShortCircuited { get; }

    bool IsResponseFromCache { get; }

    bool IsResponseRejected { get; }

    bool IsResponseSemaphoreRejected { get; }

    bool IsResponseThreadPoolRejected { get; }

    List<HystrixEventType> ExecutionEvents { get; }

    int NumberEmissions { get; }

    int NumberFallbackEmissions { get; }

    int NumberCollapsed { get; }

    int ExecutionTimeInMilliseconds { get; }

    long CommandRunStartTimeInNanos { get; }

    ExecutionResult.EventCounts EventCounts { get; }
}