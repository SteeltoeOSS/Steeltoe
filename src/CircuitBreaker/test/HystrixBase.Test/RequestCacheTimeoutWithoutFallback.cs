// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class RequestCacheTimeoutWithoutFallback : TestHystrixCommand<bool>
{
    public RequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        // we want it to timeout
    }

    protected override bool Run()
    {
        try
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, 500);
            Token.ThrowIfCancellationRequested();
        }
        catch (Exception e)
        {
            Output?.WriteLine(">>>> Sleep Interrupted: " + e.Message);
            throw;
        }

        return true;
    }

    protected override string CacheKey
    {
        get { return "A"; }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
        hystrixCommandOptions.CircuitBreakerEnabled = false;
        return hystrixCommandOptions;
    }
}
