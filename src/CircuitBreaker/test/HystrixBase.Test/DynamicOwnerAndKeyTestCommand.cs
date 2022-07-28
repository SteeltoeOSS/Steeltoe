// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class DynamicOwnerAndKeyTestCommand : TestHystrixCommand<bool>
{
    public DynamicOwnerAndKeyTestCommand(IHystrixCommandGroupKey owner, IHystrixCommandKey key)
        : base(TestPropsBuilder().SetOwner(owner).SetCommandKey(key).SetCircuitBreaker(null).SetMetrics(null))
    {
        // we specifically are NOT passing in a circuit breaker here so we test that it creates a new one correctly based on the dynamic key
    }

    protected override bool Run()
    {
        Output?.WriteLine("successfully executed");
        return true;
    }
}
