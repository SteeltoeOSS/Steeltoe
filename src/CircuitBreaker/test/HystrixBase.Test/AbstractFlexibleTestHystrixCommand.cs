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
    protected readonly ExecutionResultTest Result;
    protected readonly int ExecutionLatency;
    protected readonly CacheEnabledTest CacheEnabled;
    protected readonly object Value;

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
        Result = executionResult;
        this.ExecutionLatency = executionLatency;
        this.CacheEnabled = cacheEnabled;
        this.Value = value;
    }

    protected override int Run()
    {
        AddLatency(ExecutionLatency);
        if (Result == ExecutionResultTest.Success)
        {
            return FlexibleTestHystrixCommand.ExecuteValue;
        }
        else if (Result == ExecutionResultTest.Failure)
        {
            throw new Exception("Execution Failure for TestHystrixCommand");
        }
        else if (Result == ExecutionResultTest.HystrixFailure)
        {
            throw new HystrixRuntimeException(FailureType.CommandException, typeof(AbstractFlexibleTestHystrixCommand), "Execution Hystrix Failure for TestHystrixCommand", new Exception("Execution Failure for TestHystrixCommand"), new Exception("Fallback Failure for TestHystrixCommand"));
        }
        else if (Result == ExecutionResultTest.RecoverableError)
        {
            throw new Exception("Execution ERROR for TestHystrixCommand");
        }
        else if (Result == ExecutionResultTest.UnrecoverableError)
        {
            throw new OutOfMemoryException("Unrecoverable Error for TestHystrixCommand");
        }
        else if (Result == ExecutionResultTest.BadRequest)
        {
            throw new HystrixBadRequestException("Execution BadRequestException for TestHystrixCommand");
        }
        else
        {
            throw new Exception($"You passed in a executionResult enum that can't be represented in HystrixCommand: {Result}");
        }
    }

    protected override string CacheKey
    {
        get
        {
            if (CacheEnabled == CacheEnabledTest.Yes)
            {
                return Value.ToString();
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
                Output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " About to sleep for : " + latency);
                Time.WaitUntil(() => Token.IsCancellationRequested, latency);
                Token.ThrowIfCancellationRequested();

                Output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " Woke up from sleep!");
            }
            catch (Exception e)
            {
                Output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + e);

                // ignore and sleep some more to simulate a dependency that doesn't obey interrupts
                try
                {
                    Time.Wait(latency);
                }
                catch (Exception)
                {
                    // ignore
                }

                Output?.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + "after interruption with extra sleep");
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
