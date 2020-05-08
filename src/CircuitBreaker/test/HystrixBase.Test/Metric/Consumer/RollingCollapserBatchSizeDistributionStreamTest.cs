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
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCollapserBatchSizeDistributionStreamTest : CommandStreamTest, IDisposable
    {
        private readonly ITestOutputHelper output;
        private RollingCollapserBatchSizeDistributionStream stream;
        private IDisposable latchSubscription;

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
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
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
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // First collapser created with key will be used for all command creations
            List<Task> tasks = new List<Task>();

            var c1 = Collapser.From(output, key, 1);
            tasks.Add(c1.ExecuteAsync());
            var c2 = Collapser.From(output, key, 2);
            tasks.Add(c2.ExecuteAsync());
            var c3 = Collapser.From(output, key, 3);
            tasks.Add(c3.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
            c1.CommandCreated = false;

            var c4 = Collapser.From(output, key, 4);
            tasks.Add(c4.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
            c1.CommandCreated = false;

            var c5 = Collapser.From(output, key, 5);
            tasks.Add(c5.ExecuteAsync());
            var c6 = Collapser.From(output, key, 6);
            tasks.Add(c6.ExecuteAsync());
            var c7 = Collapser.From(output, key, 7);
            tasks.Add(c7.ExecuteAsync());
            var c8 = Collapser.From(output, key, 8);
            tasks.Add(c8.ExecuteAsync());
            var c9 = Collapser.From(output, key, 9);
            tasks.Add(c9.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
            c1.CommandCreated = false;

            var c10 = Collapser.From(output, key, 10);
            tasks.Add(c10.ExecuteAsync());
            var c11 = Collapser.From(output, key, 11);
            tasks.Add(c11.ExecuteAsync());
            var c12 = Collapser.From(output, key, 12);
            tasks.Add(c12.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

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
            List<Task> tasks = new List<Task>();

            var c1 = Collapser.From(output, key, 1);
            tasks.Add(c1.ExecuteAsync());
            var c2 = Collapser.From(output, key, 2);
            tasks.Add(c2.ExecuteAsync());
            var c3 = Collapser.From(output, key, 3);
            tasks.Add(c3.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
            c1.CommandCreated = false;

            var c4 = Collapser.From(output, key, 4);
            tasks.Add(c4.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
            c1.CommandCreated = false;

            var c5 = Collapser.From(output, key, 5);
            tasks.Add(c5.ExecuteAsync());
            var c6 = Collapser.From(output, key, 6);
            tasks.Add(c6.ExecuteAsync());
            var c7 = Collapser.From(output, key, 7);
            tasks.Add(c7.ExecuteAsync());
            var c8 = Collapser.From(output, key, 8);
            tasks.Add(c8.ExecuteAsync());
            var c9 = Collapser.From(output, key, 9);
            tasks.Add(c9.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
            c1.CommandCreated = false;

            var c10 = Collapser.From(output, key, 10);
            tasks.Add(c10.ExecuteAsync());
            var c11 = Collapser.From(output, key, 11);
            tasks.Add(c11.ExecuteAsync());
            var c12 = Collapser.From(output, key, 12);
            tasks.Add(c12.ExecuteAsync());
            Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

            Task.WaitAll(tasks.ToArray());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.Latest.GetTotalCount());
            Assert.Equal(0, stream.LatestMean);
        }
    }
}
