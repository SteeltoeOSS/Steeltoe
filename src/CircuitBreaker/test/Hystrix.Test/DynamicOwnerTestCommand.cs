// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class DynamicOwnerTestCommand : TestHystrixCommand<bool>
{
    public DynamicOwnerTestCommand(IHystrixCommandGroupKey owner)
        : base(TestPropsBuilder().SetOwner(owner))
    {
    }

    protected override bool Run()
    {
        Output?.WriteLine("successfully executed");
        return true;
    }
}
