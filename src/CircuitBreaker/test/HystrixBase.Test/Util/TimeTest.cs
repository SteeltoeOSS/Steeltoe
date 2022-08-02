// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Steeltoe.Common.Util;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test;

public class TimeTest
{
    private const int Grace = 180;

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void WaitUntil_WaitsExpectedTime()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        Time.WaitUntil(() => false, 1000);
        stopWatch.Stop();
        Assert.InRange(stopWatch.ElapsedMilliseconds, 1000 - Grace, 1000 + Grace);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void Wait_WaitsExpectedTime()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        Time.Wait(1000);
        stopWatch.Stop();
        Assert.InRange(stopWatch.ElapsedMilliseconds, 1000 - Grace, 1000 + Grace);
    }
}
