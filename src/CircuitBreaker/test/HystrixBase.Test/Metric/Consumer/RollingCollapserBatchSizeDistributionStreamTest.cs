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
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCollapserBatchSizeDistributionStreamTest : CommandStreamTest, IDisposable
    {
        private RollingCollapserBatchSizeDistributionStream stream;
        private IDisposable latchSubscription;

        private ITestOutputHelper output;

        private class LatchedObserver : TestObserverBase<CachedValuesHistogram>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public RollingCollapserBatchSizeDistributionStreamTest(ITestOutputHelper output)
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
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEmptyStreamProducesEmptyDistributions()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-A");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(10 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestBatches()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(10 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // First collapser created with key will be used for all command creations
            var c1 = Collapser.From(output, key, 1);
            c1.Observe();
            var c2 = Collapser.From(output, key, 2);
            c2.Observe();
            var c3 = Collapser.From(output, key, 3);
            c3.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
            c1.CommandCreated = false;

            var c4 = Collapser.From(output, key, 4);
            c4.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
            c1.CommandCreated = false;

            var c5 = Collapser.From(output, key, 5);
            c5.Observe();
            var c6 = Collapser.From(output, key, 6);
            c6.Observe();
            var c7 = Collapser.From(output, key, 7);
            c7.Observe();
            var c8 = Collapser.From(output, key, 8);
            c8.Observe();
            var c9 = Collapser.From(output, key, 9);
            c9.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
            c1.CommandCreated = false;

            var c10 = Collapser.From(output, key, 10);
            c10.Observe();
            var c11 = Collapser.From(output, key, 11);
            c11.Observe();
            var c12 = Collapser.From(output, key, 12);
            c12.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            // should have 4 batches: 3, 1, 5, 3
            Assert.Equal(4, stream.Latest.GetTotalCount());
            Assert.Equal(3, stream.LatestMean);
            Assert.Equal(1, stream.GetLatestPercentile(0));
            Assert.Equal(5, stream.GetLatestPercentile(100));
        }

        // by doing a take(30), all metrics should fall out of window and we should observe an empty histogram
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestBatchesAgeOut()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // First collapser created with key will be used for all command creations
            var c1 = Collapser.From(output, key, 1);
            c1.Observe();
            var c2 = Collapser.From(output, key, 2);
            c2.Observe();
            var c3 = Collapser.From(output, key, 3);
            c3.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
            c1.CommandCreated = false;

            var c4 = Collapser.From(output, key, 4);
            c4.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
            c1.CommandCreated = false;

            var c5 = Collapser.From(output, key, 5);
            c5.Observe();
            var c6 = Collapser.From(output, key, 6);
            c6.Observe();
            var c7 = Collapser.From(output, key, 7);
            c7.Observe();
            var c8 = Collapser.From(output, key, 8);
            c8.Observe();
            var c9 = Collapser.From(output, key, 9);
            c9.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
            c1.CommandCreated = false;

            var c10 = Collapser.From(output, key, 10);
            c10.Observe();
            var c11 = Collapser.From(output, key, 11);
            c11.Observe();
            var c12 = Collapser.From(output, key, 12);
            c12.Observe();
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.Latest.GetTotalCount());
            Assert.Equal(0, stream.LatestMean);
        }
    }
}
