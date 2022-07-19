// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class FlexibleTestHystrixCommandWithFallback : AbstractFlexibleTestHystrixCommand
{
    private readonly FallbackResultTest _fallbackResult;
    private readonly int _fallbackLatency;

    public FlexibleTestHystrixCommandWithFallback(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        : base(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled)
    {
        this._fallbackResult = fallbackResult;
        this._fallbackLatency = fallbackLatency;
    }

    protected override int RunFallback()
    {
        AddLatency(_fallbackLatency);
        if (_fallbackResult == FallbackResultTest.Success)
        {
            return FlexibleTestHystrixCommand.FallbackValue;
        }
        else if (_fallbackResult == FallbackResultTest.Failure)
        {
            throw new Exception("Fallback Failure for TestHystrixCommand");
        }
        else if (_fallbackResult == FallbackResultTest.Unimplemented)
        {
            return base.RunFallback();
        }
        else
        {
            throw new Exception($"You passed in a fallbackResult enum that can't be represented in HystrixCommand: {_fallbackResult}");
        }
    }
}
