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
    public class CumulativeCollapserEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private class LatchedObserver : ObserverBase<long[]>
        {
            private CountdownEvent latch;

            public LatchedObserver(CountdownEvent latch)
            {
                this.latch = latch;
            }

            protected override void OnCompletedCore()
            {
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] value)
            {
            }
        }

        private CumulativeCollapserEventCounterStream stream;
        private ITestOutputHelper output;

        public CumulativeCollapserEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();

            stream.Unsubscribe();
            CumulativeCollapserEventCounterStream.Reset();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-A");
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(latch));
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
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-B");
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(latch));

            for (int i = 0; i < 3; i++)
            {
                CommandStreamTest.Collapser.From(output, key, i).Observe();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();
            output.WriteLine("ReqLog : " + log);
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestCollapsedAndResponseFromCache()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-C");
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        // by doing a take(30), we expect all values to stay in the stream, as cumulative counters never age out of window
        [Fact]
        public void TestCollapsedAndResponseFromCacheAgeOutOfCumulativeWindow()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-D");
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        private static string CollapserEventsToStr(long[] eventCounts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (CollapserEventType eventType in CollapserEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    sb.Append(eventType).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
                }
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
