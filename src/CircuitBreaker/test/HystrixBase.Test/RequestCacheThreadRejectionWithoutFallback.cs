// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class RequestCacheThreadRejectionWithoutFallback : TestHystrixCommand<bool>
{
    private readonly CountdownEvent _completionLatch;

    public RequestCacheThreadRejectionWithoutFallback(TestCircuitBreaker circuitBreaker, CountdownEvent completionLatch)
        : base(TestPropsBuilder()
            .SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.Metrics)
            .SetThreadPool(new RequestCacheThreadRejectionWithoutFallbackThreadPool()))
    {
        _completionLatch = completionLatch;
    }

    protected override bool Run()
    {
        if (_completionLatch.Wait(1000))
        {
            throw new Exception("timed out waiting on completionLatch");
        }

        return true;
    }

    protected override string CacheKey
    {
        get { return "A"; }
    }
}
