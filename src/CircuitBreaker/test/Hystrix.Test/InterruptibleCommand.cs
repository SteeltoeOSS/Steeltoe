// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class InterruptibleCommand : TestHystrixCommand<bool>
{
    private volatile bool _hasBeenInterrupted;

    public bool HasBeenInterrupted => _hasBeenInterrupted;

    public InterruptibleCommand(TestCircuitBreaker circuitBreaker, int timeoutInMillis)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), timeoutInMillis)))
    {
    }

    public InterruptibleCommand(TestCircuitBreaker circuitBreaker)
        : this(circuitBreaker, 100)
    {
    }

    protected override bool Run()
    {
        try
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, 2000);
            Token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            Output?.WriteLine("Interrupted!");
            _hasBeenInterrupted = true;
            throw;
        }

        return _hasBeenInterrupted;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int timeoutInMillis)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeoutInMillis;
        return hystrixCommandOptions;
    }
}
