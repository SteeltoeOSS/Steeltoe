// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class CumulativeCollapserEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        private CumulativeCollapserEventCounterStream stream;
        private IDisposable latchSubscription;
        private ITestOutputHelper output;

        public CumulativeCollapserEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            latchSubscription?.Dispose();
            stream?.Unsubscribe();
            latchSubscription = null;
            stream = null;
            base.Dispose();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-A");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);

            latchSubscription = stream.Observe().Subscribe(observer);

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);

            Assert.Equal(0, stream.GetLatest(CollapserEventType.ADDED_TO_BATCH));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.BATCH_EXECUTED));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.RESPONSE_FROM_CACHE));
        }

        [Fact]
        public void TestCollapsed()
        {
            var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                tasks.Add(Collapser.From(output, key, i).ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestCollapsedAndResponseFromCache()
        {
            var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-C");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                tasks.Add(Collapser.From(output, key, i).ExecuteAsync());
                tasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
                tasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            Assert.Equal(expected, stream.Latest);
        }

        // by doing a take(30), we expect all values to stay in the stream, as cumulative counters never age out of window
        [Fact]
        public void TestCollapsedAndResponseFromCacheAgeOutOfCumulativeWindow()
        {
            var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-D");
            stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            var latch = new CountdownEvent(1);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(new LatchedObserver(output, latch));

            for (var i = 0; i < 3; i++)
            {
                Collapser.From(output, key, i).Observe();
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
                Collapser.From(output, key, i).Observe(); // same arg - should get a response from cache
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            Assert.Equal(expected, stream.Latest);
        }

        private static string CollapserEventsToStr(long[] eventCounts)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var eventType in CollapserEventTypeHelper.Values)
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
