// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class BadRequestCommand : TestHystrixCommand<bool>
{
    public BadRequestCommand(TestCircuitBreaker circuitBreaker, ExecutionIsolationStrategy isolationType)
        : base(TestPropsBuilder()
            .SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.Metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationType)))
    {
    }

    protected override bool Run()
    {
        throw new HystrixBadRequestException("Message to developer that they passed in bad data or something like that.");
    }

    protected override bool RunFallback()
    {
        return false;
    }

    protected override string CacheKey
    {
        get { return "one"; }
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
        return hystrixCommandOptions;
    }
}
