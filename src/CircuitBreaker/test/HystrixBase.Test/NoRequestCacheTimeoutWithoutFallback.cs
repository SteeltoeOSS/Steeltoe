// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class NoRequestCacheTimeoutWithoutFallback : TestHystrixCommand<bool>
{
    public NoRequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
    {
        // we want it to timeout
    }

    protected override bool Run()
    {
        try
        {
            Time.WaitUntil(() => _token.IsCancellationRequested, 500);
            _token.ThrowIfCancellationRequested();
        }
        catch (Exception e)
        {
            _output?.WriteLine(">>>> Sleep Interrupted: " + e.Message);
            throw;
        }

        return true;
    }

    protected override string CacheKey
    {
        get { return null; }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
    {
        hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
        hystrixCommandOptions.CircuitBreakerEnabled = false;
        return hystrixCommandOptions;
    }
}
