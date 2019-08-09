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

using System;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Util.Test
{
    public class TimerReferenceTest
    {
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TimerReference_CallsListenerOnTime()
        {
            Stopwatch stopWatch = new Stopwatch();
            TestListener listener = new TestListener(stopWatch);
            TimerReference timerReference = new TimerReference(listener, TimeSpan.FromMilliseconds(1000));
            stopWatch.Start();
            timerReference.Start();
            Time.WaitUntil(() => { return !stopWatch.IsRunning; }, 2000);
            Assert.InRange(stopWatch.ElapsedMilliseconds, 1000, 1000 + 200);
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
}
