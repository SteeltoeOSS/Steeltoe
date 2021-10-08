// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCollapserEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private readonly ITestOutputHelper output;
        private RollingCollapserEventCounterStream stream;
        private IDisposable latchSubscription;

        private class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
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
            latchSubscription?.Dispose();
            stream?.Unsubscribe();
            latchSubscription = null;
            stream = null;
            base.Dispose();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            var key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-A");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatest(CollapserEventType.ADDED_TO_BATCH));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.BATCH_EXECUTED));
            Assert.Equal(0, stream.GetLatest(CollapserEventType.RESPONSE_FROM_CACHE));
        }

        [Fact]
        public void TestCollapsed()
        {
            var key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cTasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync());
            }

            Task.WaitAll(cTasks.ToArray());

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
            var key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-C");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cTasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync());
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            }

            Task.WaitAll(cTasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
            Assert.Equal(expected, stream.Latest);
        }

        // by doing a take(30), we expect all values to return to 0 as they age out of rolling window
        [Fact]
        public void TestCollapsedAndResponseFromCacheAgeOutOfRollingWindow()
        {
            var key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-D");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cTasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync());
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
                cTasks.Add(Collapser.From(output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            }

            Task.WaitAll(cTasks.ToArray());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(CollapserEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[CollapserEventTypeHelper.Values.Count];
            expected[(int)CollapserEventType.BATCH_EXECUTED] = 0;
            expected[(int)CollapserEventType.ADDED_TO_BATCH] = 0;
            expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 0;
            Assert.Equal(expected, stream.Latest);
        }

        protected static string CollapserEventsToStr(long[] eventCounts)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            foreach (var eventType in CollapserEventTypeHelper.Values)
            {
                if (eventCounts[(int)eventType] > 0)
                {
                    sb.Append(eventType.ToString()).Append("->").Append(eventCounts[(int)eventType]).Append(", ");
                }
            }

            sb.Append(']');
            return sb.ToString();
        }
    }
}
