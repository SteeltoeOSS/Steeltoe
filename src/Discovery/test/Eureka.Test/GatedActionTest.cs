// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class GatedActionTest
{
    private int _timerFuncCount;

    [Fact]
    public async Task Run_Enforces_SingleActiveTask()
    {
        Interlocked.Exchange(ref _timerFuncCount, 0);
        var timedTask = new GatedAction(TimerFunc);
        await using var timer = new Timer(_ => timedTask.Run(), null, 10, 100);
        SpinWait.SpinUntil(() => false, 1.Seconds());
        Assert.Equal(1, _timerFuncCount);
    }

    private void TimerFunc()
    {
        Interlocked.Increment(ref _timerFuncCount);
        Thread.Sleep(3.Seconds());
    }
}
