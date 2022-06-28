// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestChainedCommandPrimaryCommand : TestHystrixCommand<int>
{
    public TestChainedCommandPrimaryCommand(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics))
    {
    }

    protected override int Run()
    {
        throw new Exception("primary failure");
    }

    protected override int RunFallback()
    {
        var subCmd = new TestChainedCommandSubCommand(new TestCircuitBreaker());
        return subCmd.Execute();
    }
}
