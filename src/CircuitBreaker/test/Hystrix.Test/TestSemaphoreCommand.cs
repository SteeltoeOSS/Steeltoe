// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestSemaphoreCommand : TestHystrixCommand<bool>
{
    public const int ResultSuccess = 1;
    public const int ResultFailure = 2;
    public const int ResultBadRequestException = 3;
    public const int FallbackSuccess = 10;
    public const int FallbackNotImplemented = 11;
    public const int FallbackFailure = 12;
    private readonly int _executionSleep;

    public readonly int ResultBehavior;
    public readonly int FallbackBehavior;

    public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, int executionSleep, int resultBehavior, int fallbackBehavior)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), executionSemaphoreCount)))
    {
        _executionSleep = executionSleep;
        ResultBehavior = resultBehavior;
        FallbackBehavior = fallbackBehavior;
    }

    public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, int executionSleep, int resultBehavior, int fallbackBehavior)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics).SetExecutionSemaphore(semaphore)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        _executionSleep = executionSleep;
        ResultBehavior = resultBehavior;
        FallbackBehavior = fallbackBehavior;
    }

    protected override bool Run()
    {
        Time.Wait(_executionSleep);

        if (ResultBehavior == ResultSuccess)
        {
            return true;
        }

        if (ResultBehavior == ResultFailure)
        {
            throw new Exception("TestSemaphoreCommand failure");
        }

        if (ResultBehavior == ResultBadRequestException)
        {
            throw new HystrixBadRequestException("TestSemaphoreCommand BadRequestException");
        }

        throw new InvalidOperationException("Didn't use a proper enum for result behavior");
    }

    protected override bool RunFallback()
    {
        if (FallbackBehavior == FallbackSuccess)
        {
            return false;
        }

        if (FallbackBehavior == FallbackFailure)
        {
            throw new Exception("fallback failure");
        }

        // FALLBACK_NOT_IMPLEMENTED
        return base.RunFallback();
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int executionSemaphoreCount)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        hystrixCommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests = executionSemaphoreCount;
        return hystrixCommandOptions;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        return hystrixCommandOptions;
    }
}
