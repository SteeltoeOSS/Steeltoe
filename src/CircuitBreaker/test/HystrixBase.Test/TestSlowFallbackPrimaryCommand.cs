// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestSlowFallbackPrimaryCommand : TestHystrixCommand<int>
{
    public TestSlowFallbackPrimaryCommand(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics))
    {
    }

    protected override int Run()
    {
        throw new Exception("primary failure");
    }

    protected override int RunFallback()
    {
        try
        {
            Time.WaitUntil(() => _token.IsCancellationRequested, 1500);
            _token.ThrowIfCancellationRequested();

            return 1;
        }
        catch (Exception)
        {
            _output?.WriteLine("Caught Interrupted Exception");
        }

        return -1;
    }
}
