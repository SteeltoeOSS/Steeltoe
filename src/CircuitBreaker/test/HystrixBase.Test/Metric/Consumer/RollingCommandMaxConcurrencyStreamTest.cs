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
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCommandMaxConcurrencyStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Command-Concurrency");
        private RollingCommandMaxConcurrencyStream stream;
        private IDisposable latchSubscription;

        private class LatchedObserver : TestObserverBase<int>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        private ITestOutputHelper output;

        public RollingCommandMaxConcurrencyStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;

            RollingCommandMaxConcurrencyStream.Reset();
            HystrixCommandStartStream.Reset();
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
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-A");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 500), "Stream failed to start");

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestStartsAndEndsInSameBucketProduceValue()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);

            Task t1 = cmd1.ExecuteAsync();
            Task t2 = cmd2.ExecuteAsync();
            Task.WaitAll(t1, t2);

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.LatestRollingMax);
        }

        /***
         * 3 Commands,
         * Command 1 gets started in Bucket A and not completed until Bucket B
         * Commands 2 and 3 both start and end in Bucket B, and there should be a max-concurrency of 3
         */
        [Fact]
        public void TestOneCommandCarriesOverToNextBucket()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-C");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 250);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Task t2 = cmd2.ExecuteAsync();
            Task t3 = cmd3.ExecuteAsync();

            Task.WaitAll(t1, t2, t3);

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //     A    |    B    |    C    |    D    |    E    |
        // 1:  [-------------------------------]
        // 2:          [-------------------------------]
        // 3:                      [--]
        // 4:                              [--]
        //
        // Max concurrency should be 3
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBuckets()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-D");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t2 = cmd2.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t3 = cmd3.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t4 = cmd4.ExecuteAsync();

            Task.WaitAll(t1, t2, t3, t4);
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //      A    |    B    |    C    |    D    |    E    |
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-E");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t2 = cmd2.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t3 = cmd3.ExecuteAsync();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 100, 500, output), "Latch took to long to update");
            Task t4 = cmd4.ExecuteAsync();

            Task.WaitAll(t1, t2, t3, t4);

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutResponseFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-F");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 40);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            Task t1 = cmd1.ExecuteAsync();
            Time.Wait(5);
            Task t2 = cmd2.ExecuteAsync();
            Task t3 = cmd3.ExecuteAsync();
            Task t4 = cmd4.ExecuteAsync();
            Task.WaitAll(t1, t2, t3, t4);

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutShortCircuits()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-G");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // after 3 failures, next command should short-circuit.
            // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
            // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
            Command failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            Command failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            Command failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE);

            List<Command> shortCircuited = new List<Command>();

            for (int i = 0; i < 20; i++)
            {
                shortCircuited.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0));
            }

            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 250, output), "Health count stream update took to long");

            List<Task> tasks = new List<Task>();
            foreach (Command cmd in shortCircuited)
            {
                tasks.Add(cmd.ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-H");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently on different caller threads should saturate semaphore
            // once these are in-flight, execute 10 more concurrently on new caller threads.
            // since these are semaphore-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE));
            }

            List<Task> sattasks = new List<Task>();
            foreach (Command saturatingCmd in saturators)
            {
                sattasks.Add(Task.Run(() => saturatingCmd.Execute()));
            }

            await Task.Delay(50);

            foreach (Command rejectedCmd in rejected)
            {
                await Task.Run(() => rejectedCmd.Execute());
            }

            Task.WaitAll(sattasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(10, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutThreadPoolRejections()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-I");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently should saturate the Hystrix threadpool
            // once these are in-flight, execute 10 more concurrently
            // since these are threadpool-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100));
            }

            List<Task> tasks = new List<Task>();
            foreach (Command saturatingCmd in saturators)
            {
                tasks.Add(saturatingCmd.ExecuteAsync());
            }

            Time.Wait(30);

            foreach (Command rejectedCmd in rejected)
            {
                tasks.Add(rejectedCmd.ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(10, stream.LatestRollingMax);
        }
    }
}
