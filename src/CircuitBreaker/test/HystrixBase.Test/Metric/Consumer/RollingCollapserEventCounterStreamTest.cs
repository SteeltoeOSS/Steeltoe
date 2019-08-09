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

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCollapserEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private RollingCollapserEventCounterStream stream;
        private ITestOutputHelper output;

        private class LatchedObserver : ObserverBase<long[]>
        {
            private CountdownEvent latch;
            private ITestOutputHelper output;

            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            {
                this.latch = latch;
                this.output = output;
            }

            protected override void OnCompletedCore()
            {
                output.WriteLine("OnCompletedCore @ " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId);
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] eventCounts)
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + CollapserEventsToStr(eventCounts) + " " + Thread.CurrentThread.ManagedThreadId);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }
        }

        private static LatchedObserver GetSubscriber(ITestOutputHelper output, CountdownEvent latch)
        {
            return new LatchedObserver(output, latch);
        }

        public RollingCollapserEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            RollingCollapserEventCounterStream.Reset();
            HystrixCollapserEventStream.Reset();
        }

        public override void Dispose()
        {
            base.Dispose();
            stream.Unsubscribe();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-A");
            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatest(CollapserEventType.ADDED_TO_BATCH));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.BATCH_EXECUTED));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.RESPONSE_FROM_CACHE));
        }

        [Fact]
        public void TestCollapsed()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-B");
            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestCollapsedAndResponseFromCache()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-C");
            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            Assert.Equal(expected, stream.Latest);
        }

        // by doing a take(30), we expect all values to return to 0 as they age out of rolling window
        [Fact]
        public void TestCollapsedAndResponseFromCacheAgeOutOfRollingWindow()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-D");
            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(output, latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 0;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 0;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 0;
            Assert.Equal(expected, stream.Latest);
        }

        protected static string CollapserEventsToStr(long[] eventCounts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (CollapserEventType eventType in CollapserEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    sb.Append(eventType.ToString()).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
