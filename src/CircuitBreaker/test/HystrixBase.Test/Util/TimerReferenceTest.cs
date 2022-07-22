// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test;

public class TimerReferenceTest
{
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TimerReference_CallsListenerOnTime()
    {
        var stopWatch = new Stopwatch();
        var listener = new TestListener(stopWatch);
        var timerReference = new TimerReference(listener, TimeSpan.FromMilliseconds(1000));
        stopWatch.Start();
        timerReference.Start();
        Time.WaitUntil(() => { return !stopWatch.IsRunning; }, 2000);
        Assert.InRange(stopWatch.ElapsedMilliseconds, 950, 1000 + 200);
    }

    private class TestListener : ITimerListener
    {
        private readonly Stopwatch stopwatch;

        public TestListener(Stopwatch stopwatch)
        {
            this.stopwatch = stopwatch;
        }

        public int IntervalTimeInMilliseconds => throw new NotImplementedException();

        public void Tick()
        {
            stopwatch.Stop();
        }
    }
}