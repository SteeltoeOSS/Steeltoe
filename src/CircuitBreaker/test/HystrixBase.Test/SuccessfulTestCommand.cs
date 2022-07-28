// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SuccessfulTestCommand : TestHystrixCommand<bool>
{
    public SuccessfulTestCommand()
        : this(HystrixCommandOptionsTest.GetUnitTestOptions())
    {
    }

    public SuccessfulTestCommand(HystrixCommandOptions properties)
        : base(TestPropsBuilder().SetCommandOptionDefaults(properties))
    {
    }

    protected override bool Run()
    {
        return true;
    }
}
