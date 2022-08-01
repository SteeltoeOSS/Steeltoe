// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class KnownFailureTestCommandWithFallback : TestHystrixCommand<bool>
{
    public KnownFailureTestCommandWithFallback(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder(circuitBreaker).SetMetrics(circuitBreaker.Metrics))
    {
    }

    public KnownFailureTestCommandWithFallback(TestCircuitBreaker circuitBreaker, bool fallbackEnabled)
        : base(TestPropsBuilder(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), fallbackEnabled)))
    {
    }

    protected override bool Run()
    {
        // output.WriteLine("*** simulated failed execution ***");
        throw new Exception("we failed with a simulated issue");
    }

    protected override bool RunFallback()
    {
        return false;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, bool fallbackEnabled)
    {
        hystrixCommandOptions.FallbackEnabled = fallbackEnabled;
        return hystrixCommandOptions;
    }
}
