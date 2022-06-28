// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestSemaphoreCommandWithFallback : TestHystrixCommand<bool>
{
    private readonly int _executionSleep;
    private readonly bool _runFallback;

    public TestSemaphoreCommandWithFallback(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, int executionSleep, bool runFallback)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), executionSemaphoreCount)))
    {
        _executionSleep = executionSleep;
        _runFallback = runFallback;
    }

    protected override bool Run()
    {
        Time.Wait(_executionSleep);

        return true;
    }

    protected override bool RunFallback()
    {
        return _runFallback;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int executionSemaphoreCount)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
        hystrixCommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests = executionSemaphoreCount;
        return hystrixCommandOptions;
    }
}
