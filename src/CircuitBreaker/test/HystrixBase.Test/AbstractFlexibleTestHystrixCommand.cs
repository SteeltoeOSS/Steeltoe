// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.Common.Util;
using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal abstract class AbstractFlexibleTestHystrixCommand : TestHystrixCommand<int>
{
    protected readonly ExecutionResultTest result;
    protected readonly int executionLatency;
    protected readonly CacheEnabledTest cacheEnabled;
    protected readonly object value;

    protected AbstractFlexibleTestHystrixCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        : base(TestPropsBuilder(circuitBreaker)
            .SetCommandKey(commandKey)
            .SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.Metrics)
            .SetThreadPool(threadPool)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationStrategy, timeout, !circuitBreakerDisabled))
            .SetExecutionSemaphore(executionSemaphore)
            .SetFallbackSemaphore(fallbackSemaphore))
    {
        result = executionResult;
        this.executionLatency = executionLatency;
        this.cacheEnabled = cacheEnabled;
        this.value = value;
    }

    protected override int Run()
    {
        AddLatency(executionLatency);
        if (result == ExecutionResultTest.SUCCESS)
        {
            return FlexibleTestHystrixCommand.EXECUTE_VALUE;
        }
        else if (result == ExecutionResultTest.FAILURE)
        {
            throw new Exception("Execution Failure for TestHystrixCommand");
        }
        else if (result == ExecutionResultTest.HYSTRIX_FAILURE)
        {
            throw new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, typeof(AbstractFlexibleTestHystrixCommand), "Execution Hystrix Failure for TestHystrixCommand", new Exception("Execution Failure for TestHystrixCommand"), new Exception("Fallback Failure for TestHystrixCommand"));
        }
        else if (result == ExecutionResultTest.RECOVERABLE_ERROR)
        {
            throw new Exception("Execution ERROR for TestHystrixCommand");
        }
        else if (result == ExecutionResultTest.UNRECOVERABLE_ERROR)
        {
            throw new OutOfMemoryException("Unrecoverable Error for TestHystrixCommand");
        }
        else if (result == ExecutionResultTest.BAD_REQUEST)
        {
            throw new HystrixBadRequestException("Execution BadRequestException for TestHystrixCommand");
        }
        else
        {
            throw new Exception($"You passed in a executionResult enum that can't be represented in HystrixCommand: {result}");
        }
    }

    protected override string CacheKey
    {
        get
        {
            if (cacheEnabled == CacheEnabledTest.YES)
            {
                return value.ToString();
            }
            else
            {
                return null;
            }
        }
    }

    protected void AddLatency(int latency)
    {
        if (latency > 0)
        {
            try
            {
                _output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " About to sleep for : " + latency);
                Time.WaitUntil(() => _token.IsCancellationRequested, latency);
                _token.ThrowIfCancellationRequested();

                _output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " Woke up from sleep!");
            }
            catch (Exception e)
            {
                _output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + e);

                // ignore and sleep some more to simulate a dependency that doesn't obey interrupts
                try
                {
                    Time.Wait(latency);
                }
                catch (Exception)
                {
                    // ignore
                }

                _output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + "after interruption with extra sleep");
                throw;
            }
        }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationStrategy, int timeout, bool circuitBreakerDisabled)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = isolationStrategy;
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
        hystrixCommandOptions.CircuitBreakerEnabled = circuitBreakerDisabled;
        return hystrixCommandOptions;
    }
}
