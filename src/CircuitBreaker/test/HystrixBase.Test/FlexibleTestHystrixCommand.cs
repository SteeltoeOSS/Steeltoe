// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal static class FlexibleTestHystrixCommand
{
    public static int ExecuteValue = 1;
    public static int FallbackValue = 11;

    public static AbstractFlexibleTestHystrixCommand From(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy,
        ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker,
        IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore,
        SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
    {
        if (fallbackResult.Equals(FallbackResultTest.Unimplemented))
        {
            return new FlexibleTestHystrixCommandNoFallback(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool,
                timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
        }

        var cmd = new FlexibleTestHystrixCommandWithFallback(commandKey, isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency,
            circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled)
        {
            IsFallbackUserDefined = true
        };

        return cmd;
    }
}
