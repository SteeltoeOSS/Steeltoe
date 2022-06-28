// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.Common.Util;
using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestSemaphoreCommand : TestHystrixCommand<bool>
{
    public const int RESULT_SUCCESS = 1;
    public const int RESULT_FAILURE = 2;
    public const int RESULT_BAD_REQUEST_EXCEPTION = 3;
    public const int FALLBACK_SUCCESS = 10;
    public const int FALLBACK_NOT_IMPLEMENTED = 11;
    public const int FALLBACK_FAILURE = 12;

    public readonly int ResultBehavior;
    public readonly int FallbackBehavior;
    private readonly int _executionSleep;

    public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, int executionSleep, int resultBehavior, int fallbackBehavior)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), executionSemaphoreCount)))
    {
        _executionSleep = executionSleep;
        ResultBehavior = resultBehavior;
        FallbackBehavior = fallbackBehavior;
    }

    public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, int executionSleep, int resultBehavior, int fallbackBehavior)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetExecutionSemaphore(semaphore)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        _executionSleep = executionSleep;
        ResultBehavior = resultBehavior;
        FallbackBehavior = fallbackBehavior;
    }

    protected override bool Run()
    {
        Time.Wait(_executionSleep);

        if (ResultBehavior == RESULT_SUCCESS)
        {
            return true;
        }
        else if (ResultBehavior == RESULT_FAILURE)
        {
            throw new Exception("TestSemaphoreCommand failure");
        }
        else if (ResultBehavior == RESULT_BAD_REQUEST_EXCEPTION)
        {
            throw new HystrixBadRequestException("TestSemaphoreCommand BadRequestException");
        }
        else
        {
            throw new InvalidOperationException("Didn't use a proper enum for result behavior");
        }
    }

    protected override bool RunFallback()
    {
        if (FallbackBehavior == FALLBACK_SUCCESS)
        {
            return false;
        }
        else if (FallbackBehavior == FALLBACK_FAILURE)
        {
            throw new Exception("fallback failure");
        }
        else
        {
            // FALLBACK_NOT_IMPLEMENTED
            return base.RunFallback();
        }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int executionSemaphoreCount)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
        hystrixCommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests = executionSemaphoreCount;
        return hystrixCommandOptions;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
        return hystrixCommandOptions;
    }
}
