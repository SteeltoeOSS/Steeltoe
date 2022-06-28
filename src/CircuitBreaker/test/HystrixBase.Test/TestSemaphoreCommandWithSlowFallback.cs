// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestSemaphoreCommandWithSlowFallback : TestHystrixCommand<bool>
{
    private readonly int _fallbackSleep;

    public TestSemaphoreCommandWithSlowFallback(TestCircuitBreaker circuitBreaker, int fallbackSemaphoreExecutionCount, int fallbackSleep)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), fallbackSemaphoreExecutionCount)))
    {
        _fallbackSleep = fallbackSleep;
    }

    protected override bool Run()
    {
        throw new Exception("run fails");
    }

    protected override bool RunFallback()
    {
        Time.Wait(_fallbackSleep);

        return true;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int fallbackSemaphoreExecutionCount)
    {
        hystrixCommandOptions.FallbackIsolationSemaphoreMaxConcurrentRequests = fallbackSemaphoreExecutionCount;

        // hystrixCommandOptions.ExecutionIsolationThreadInterruptOnTimeout = false;
        return hystrixCommandOptions;
    }
}
