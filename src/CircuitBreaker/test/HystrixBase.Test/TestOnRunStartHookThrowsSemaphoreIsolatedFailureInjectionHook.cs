// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook : HystrixCommandExecutionHook
{
    private readonly AtomicBoolean _onThreadStartInvoked;
    private readonly AtomicBoolean _onThreadCompleteInvoked;

    public TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook(AtomicBoolean onThreadStartInvoked, AtomicBoolean onThreadCompleteInvoked)
    {
        _onThreadStartInvoked = onThreadStartInvoked;
        _onThreadCompleteInvoked = onThreadCompleteInvoked;
    }

    public override void OnExecutionStart(IHystrixInvokable commandInstance)
    {
        throw new HystrixRuntimeException(FailureType.CommandException, commandInstance.GetType(), "Injected Failure", null, null);
    }

    public override void OnThreadStart(IHystrixInvokable commandInstance)
    {
        _onThreadStartInvoked.Value = true;
        base.OnThreadStart(commandInstance);
    }

    public override void OnThreadComplete(IHystrixInvokable commandInstance)
    {
        _onThreadCompleteInvoked.Value = true;
        base.OnThreadComplete(commandInstance);
    }
}
