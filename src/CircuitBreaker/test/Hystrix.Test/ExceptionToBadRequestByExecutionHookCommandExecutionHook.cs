// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class ExceptionToBadRequestByExecutionHookCommandExecutionHook : TestableExecutionHook
{
    public override Exception OnExecutionError(IHystrixInvokable commandInstance, Exception e)
    {
        base.OnExecutionError(commandInstance, e);
        return new HystrixBadRequestException("autoconverted exception", e);
    }
}
