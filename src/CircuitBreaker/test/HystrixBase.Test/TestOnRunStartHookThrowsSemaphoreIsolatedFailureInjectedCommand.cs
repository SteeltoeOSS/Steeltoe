// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand : TestHystrixCommand<int>
{
    private readonly AtomicBoolean _executionAttempted;

    public TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand(ExecutionIsolationStrategy isolationStrategy, AtomicBoolean executionAttempted,
        HystrixCommandExecutionHook failureInjectionHook)
        : base(TestPropsBuilder().SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationStrategy)),
            failureInjectionHook)
    {
        _executionAttempted = executionAttempted;
    }

    protected override int Run()
    {
        _executionAttempted.Value = true;
        return 3;
    }

    private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
    {
        hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
        return hystrixCommandOptions;
    }
}
