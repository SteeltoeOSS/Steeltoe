// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class FlexibleTestHystrixCommandNoFallback : AbstractFlexibleTestHystrixCommand
{
    public FlexibleTestHystrixCommandNoFallback(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy,
        ExecutionResultTest executionResult, int executionLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout,
        CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        : base(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore,
            fallbackSemaphore, circuitBreakerDisabled)
    {
    }
}
