// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class InterruptibleCommand : TestHystrixCommand<bool>
{
    public InterruptibleCommand(TestCircuitBreaker circuitBreaker, bool shouldInterrupt, bool shouldInterruptOnCancel, int timeoutInMillis)
        : base(TestPropsBuilder()
            .SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), shouldInterrupt, shouldInterruptOnCancel, timeoutInMillis)))
    {
    }

    public InterruptibleCommand(TestCircuitBreaker circuitBreaker, bool shouldInterrupt)
        : this(circuitBreaker, shouldInterrupt, false, 100)
    {
    }

    private volatile bool _hasBeenInterrupted;

    public bool HasBeenInterrupted
    {
        get { return _hasBeenInterrupted; }
    }

    protected override bool Run()
    {
        try
        {
            Time.WaitUntil(() => _token.IsCancellationRequested, 2000);
            _token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            _output?.WriteLine("Interrupted!");
            _hasBeenInterrupted = true;
            throw;
        }

        return _hasBeenInterrupted;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, bool shouldInterrupt, bool shouldInterruptOnCancel, int timeoutInMillis)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeoutInMillis;
        return hystrixCommandOptions;
    }
}
