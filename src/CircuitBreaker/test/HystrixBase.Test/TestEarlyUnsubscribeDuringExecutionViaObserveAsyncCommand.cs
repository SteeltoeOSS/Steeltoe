// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand : HystrixCommand<bool>
{
    public TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand()
        : base(new HystrixCommandOptions
        {
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC")
        })
    {
    }

    protected override bool Run()
    {
        Time.WaitUntil(() => Token.IsCancellationRequested, 500);
        Token.ThrowIfCancellationRequested();
        return true;
    }
}
