// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class LatchedSemaphoreCommand : TestHystrixCommand<bool>
{
    private readonly CountdownEvent _startLatch;
    private readonly CountdownEvent _waitLatch;

    public LatchedSemaphoreCommand(TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, CountdownEvent startLatch, CountdownEvent waitLatch)
        : this("Latched", circuitBreaker, semaphore, startLatch, waitLatch)
    {
    }

    public LatchedSemaphoreCommand(string commandName, TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, CountdownEvent startLatch,
        CountdownEvent waitLatch)
        : base(TestPropsBuilder().SetCommandKey(HystrixCommandKeyDefault.AsKey(commandName)).SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.Metrics).SetExecutionSemaphore(semaphore)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        _startLatch = startLatch;
        _waitLatch = waitLatch;
    }

    protected override bool Run()
    {
        // signals caller that run has started
        _startLatch.SignalEx();

        try
        {
            // waits for caller to countDown latch
            _waitLatch.Wait();
        }
        catch (Exception)
        {
            // e.printStackTrace();
            return false;
        }

        return true;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        hystrixCommandOptions.CircuitBreakerEnabled = false;
        return hystrixCommandOptions;
    }
}
