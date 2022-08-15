// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class ExceptionToBadRequestByExecutionHookCommand : TestHystrixCommand<bool>
{
    protected override string CacheKey => "nein";

    public ExceptionToBadRequestByExecutionHookCommand(TestCircuitBreaker circuitBreaker, ExecutionIsolationStrategy isolationType)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.Metrics)
            .SetExecutionHook(new ExceptionToBadRequestByExecutionHookCommandExecutionHook())
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationType)))
    {
    }

    protected override bool Run()
    {
        throw new BusinessException("invalid input by the user");
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
        return hystrixCommandOptions;
    }
}
