// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test
{
    public class HystrixTimerTest : IDisposable
    {
        private ITestOutputHelper output;

        public HystrixTimerTest(ITestOutputHelper output)
        {
            HystrixTimer timer = HystrixTimer.GetInstance();
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
            HystrixTimer timer = HystrixTimer.GetInstance();
            TestListener l1 = new TestListener(50, "A");
            timer.AddTimerListener(l1);

            TestListener l2 = new TestListener(50, "B");
            timer.AddTimerListener(l2);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have 7 or more 50ms ticks within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.True(l1.TickCount.Value > 7);
            Assert.True(l2.TickCount.Value > 7);
        }

        [Fact]
        public void TestSingleCommandMultipleIntervals()
        {
            HystrixTimer timer = HystrixTimer.GetInstance();
            TestListener l1 = new TestListener(100, "A");
            timer.AddTimerListener(l1);

            TestListener l2 = new TestListener(10, "B");
            timer.AddTimerListener(l2);

            TestListener l3 = new TestListener(25, "C");
            timer.AddTimerListener(l3);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have 3 or more 100ms ticks within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            Assert.True(l1.TickCount.Value >= 3);

            // but it can't be more than 6
            Assert.True(l1.TickCount.Value < 6);

            // we should have 30 or more 10ms ticks within 500ms
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.True(l2.TickCount.Value > 30);
            Assert.True(l2.TickCount.Value < 550);

            // we should have 15-20 25ms ticks within 500ms
            output.WriteLine("l3 ticks: " + l3.TickCount.Value);
            Assert.True(l3.TickCount.Value > 14);
            Assert.True(l3.TickCount.Value < 25);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleCommandRemoveListener()
        {
            HystrixTimer timer = HystrixTimer.GetInstance();
            TestListener l1 = new TestListener(50, "A");
            timer.AddTimerListener(l1);

            TestListener l2 = new TestListener(50, "B");
            TimerReference l2ref = timer.AddTimerListener(l2);

            try
            {
                Time.Wait(500);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
            }

            // we should have 7 or more 50ms ticks within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);
            Assert.True(l1.TickCount.Value > 7);
            Assert.True(l2.TickCount.Value > 7);

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

            // we should have 7 or more 50ms ticks within 500ms
            output.WriteLine("l1 ticks: " + l1.TickCount.Value);
            output.WriteLine("l2 ticks: " + l2.TickCount.Value);

            // l1 should continue ticking
            Assert.True(l1.TickCount.Value > 7);

            // we should have no ticks on l2 because we removed it
            output.WriteLine("tickCount.Value: " + l2.TickCount.Value + " on l2: " + l2);
            Assert.Equal(0, l2.TickCount.Value);
        }

        [Fact]
        public void TestReset()
        {
            HystrixTimer timer = HystrixTimer.GetInstance();
            TestListener l1 = new TestListener(50, "A");
            TimerReference tref = timer.AddTimerListener(l1);

            Task ex = tref._timerTask;

            Assert.False(ex.IsCanceled);

            Time.WaitUntil(() => { return ex.Status == TaskStatus.Running; }, 200);

            // perform reset which should shut it down
            HystrixTimer.Reset();

            Time.Wait(50);

            Assert.True(ex.IsCompleted);
            Assert.Null(tref._timerTask);

            // assert it starts up again on use
            TestListener l2 = new TestListener(50, "A");
            TimerReference tref2 = timer.AddTimerListener(l2);

            Task ex2 = tref2._timerTask;

            Assert.False(ex2.IsCanceled);

            // reset again to shutdown what we just started
            HystrixTimer.Reset();

            // try resetting again to make sure it's idempotent (ie. doesn't blow up on an NPE)
            HystrixTimer.Reset();
        }

        private class TestListener : ITimerListener
        {
            public AtomicInteger TickCount = new AtomicInteger();
            private int interval;

            public TestListener(int interval, string value)
            {
                this.interval = interval;
            }

            public void Tick()
            {
                TickCount.IncrementAndGet();
            }

            public int IntervalTimeInMilliseconds
            {
                get { return interval; }
            }
        }
    }
}
