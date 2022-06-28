// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class RequestCacheNullPointerExceptionCase : TestHystrixCommand<bool>
{
    public RequestCacheNullPointerExceptionCase(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        // we want it to timeout
    }

    protected override bool Run()
    {
        Time.WaitUntil(() => _token.IsCancellationRequested, 500);
        _token.ThrowIfCancellationRequested();

        return true;
    }

    protected override bool RunFallback()
    {
        return false;
    }

    protected override string CacheKey
    {
        get { return "A"; }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
        return hystrixCommandOptions;
    }
}
