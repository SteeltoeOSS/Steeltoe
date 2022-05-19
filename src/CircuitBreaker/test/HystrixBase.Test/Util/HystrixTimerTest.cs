// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.Common.Util;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test
{
    public class HystrixTimerTest : IDisposable
    {
        private readonly ITestOutputHelper output;

        public HystrixTimerTest(ITestOutputHelper output)
        {
            _ = HystrixTimer.GetInstance();
            HystrixTimer.Reset();
            HystrixPlugins.Reset();
            this.output = output;
        }

        public void Dispose()
        {
            HystrixPlugins.Reset();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleCommandSingleInterval()
        {
            var timer = HystrixTimer.GetInstance();
            var l1 = new TestListener(30);
            timer.AddTimerListener(l1);

            var l2 = new TestListener(30);
            timer.AddTimerListener(l2);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have 7 or more 30ms ticks within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.True(l1.TickCount.Value > 7, "l1 failed to execute 7 ticks in a window that could fit 12");
            Assert.True(l2.TickCount.Value > 7, "l2 failed to execute 7 ticks in a window that could fit 12");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleCommandMultipleIntervals()
        {
            var timer = HystrixTimer.GetInstance();
            var l1 = new TestListener(100);
            timer.AddTimerListener(l1);

            var l2 = new TestListener(10);
            timer.AddTimerListener(l2);

            var l3 = new TestListener(25);
            timer.AddTimerListener(l3);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have more than 2 ticks @ 100ms within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            Assert.InRange(l1.TickCount.Value, 2, 8);

            // we should have 25 - 55 10ms ticks within 500ms
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.InRange(l2.TickCount.Value, 8, 60);

            // we should have 15-20 25ms ticks within 500ms
            output.WriteLine("l3 ticks: " + l3.TickCount.Value);
            Assert.InRange(l3.TickCount.Value, 8, 25);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleCommandRemoveListener()
        {
            var timer = HystrixTimer.GetInstance();
            var l1 = new TestListener(50);
            timer.AddTimerListener(l1);

            var l2 = new TestListener(50);
            var l2ref = timer.AddTimerListener(l2);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have more than 5 ticks @ 50ms within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.True(l1.TickCount.Value > 5, "l1 failed to execute more than 5 ticks in a window that could fit 10");
            Assert.True(l2.TickCount.Value > 5, "l2 failed to execute more than 5 ticks in a window that could fit 10");

            // remove l2
            l2ref.Dispose();

            // reset counts
            l1.TickCount.Value = 0;
            l2.TickCount.Value = 0;

            // wait for time to pass again
            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have more than 5 ticks @ 50ms within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);

            // l1 should continue ticking
            Assert.True(l1.TickCount.Value > 5, "l1 failed to execute more than 5 ticks in a window that could fit 10");

            // we should have no ticks on l2 because we removed it
            output.WriteLine("tickCount.Value: " + l2.TickCount.Value + " on l2: " + l2);
            Assert.Equal(0, l2.TickCount.Value);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestReset()
        {
            var timer = HystrixTimer.GetInstance();
            var l1 = new TestListener(50);
            var tref = timer.AddTimerListener(l1);

            var ex = tref._timerTask;

            Assert.False(ex.IsCanceled);

            Time.WaitUntil(() => ex.Status == TaskStatus.Running, 200);

            // perform reset which should shut it down
            HystrixTimer.Reset();

            Time.Wait(50);

            Assert.True(ex.IsCompleted);
            Assert.Null(tref._timerTask);

            // assert it starts up again on use
            var l2 = new TestListener(50);
            var tref2 = timer.AddTimerListener(l2);

            var ex2 = tref2._timerTask;

            Assert.False(ex2.IsCanceled);

            // reset again to shutdown what we just started
            HystrixTimer.Reset();

            // try resetting again to make sure it's idempotent (ie. doesn't blow up on an NPE)
            HystrixTimer.Reset();
        }

        private class TestListener : ITimerListener
        {
            public AtomicInteger TickCount = new ();

            public TestListener(int interval)
            {
                IntervalTimeInMilliseconds = interval;
            }

            public void Tick()
            {
                TickCount.IncrementAndGet();
            }

            public int IntervalTimeInMilliseconds { get; private set; }
        }
    }
}
