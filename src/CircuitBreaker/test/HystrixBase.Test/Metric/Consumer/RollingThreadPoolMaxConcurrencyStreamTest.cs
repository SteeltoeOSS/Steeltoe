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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingThreadPoolMaxConcurrencyStreamTest : CommandStreamTest, IDisposable
    {
        private RollingThreadPoolMaxConcurrencyStream stream;
        private IDisposable latchSubscription;
        private ITestOutputHelper output;

        private class LatchedObserver : TestObserverBase<int>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public RollingThreadPoolMaxConcurrencyStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;

            HystrixThreadPoolStartStream.Reset();
            RollingThreadPoolMaxConcurrencyStream.Reset();
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
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-A");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestStartsAndEndsInSameBucketProduceValue()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-B");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-B");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 50);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 40);

            Task t1 = cmd1.ExecuteAsync();
            Task t2 = cmd2.ExecuteAsync();

            Task.WaitAll(t1, t2);
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(2, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestStartsAndEndsInSameBucketSemaphoreIsolated()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-C");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-C");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-C");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10, ExecutionIsolationStrategy.SEMAPHORE);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 14, ExecutionIsolationStrategy.SEMAPHORE);

            Task t1 = cmd1.ExecuteAsync();
            Task t2 = cmd2.ExecuteAsync();

            Task.WaitAll(t1, t2);
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            // since commands run in semaphore isolation, they are not tracked by threadpool metrics
            Assert.Equal(0, stream.LatestRollingMax);
        }

        /***
         * 3 Commands,
         * Command 1 gets started in Bucket A and not completed until Bucket B
         * Commands 2 and 3 both start and end in Bucket B, and there should be a max-concurrency of 3
         */
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestOneCommandCarriesOverToNextBucket()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-D");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-D");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-D");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 560);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 50);
            var cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 75);

            Task t1 = cmd1.ExecuteAsync();

            // Time.Wait(150); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 500, output), "Stream update took to long");
            Task t2 = cmd2.ExecuteAsync();
            Task t3 = cmd3.ExecuteAsync();

            Task.WaitAll(t1, t2, t3);
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //      A    |    B    |    C    |    D    |    E    |
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBuckets()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-E");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-E");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-E");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);
            var cmd4 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t2 = cmd2.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t3 = cmd3.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t4 = cmd4.ExecuteAsync();

            Task.WaitAll(t1, t2, t3, t4);
            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //      A    |    B    |    C    |    D    |    E    |
        //  1:  [-------------------------------]              ThreadPool x
        //  2:          [-------------------------------]                 y
        //  3:                      [--]                                  x
        //  4:                              [--]                          x
        //  Same input data as above test, just that command 2 runs in a separate threadpool, so concurrency should not get tracked
        //  Max concurrency should be 2 for x
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsForMultipleThreadPools()
        {
            var groupKeyX = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-X");
            var groupKeyY = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-Y");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-X");
            var keyX = HystrixCommandKeyDefault.AsKey("RollingConcurrency-X");
            var keyY = HystrixCommandKeyDefault.AsKey("RollingConcurrency-Y");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKeyX, keyX, HystrixEventType.SUCCESS, 300);
            var cmd2 = Command.From(groupKeyY, keyY, HystrixEventType.SUCCESS, 300);
            var cmd3 = Command.From(groupKeyX, keyY, HystrixEventType.SUCCESS, 10);
            var cmd4 = Command.From(groupKeyX, keyY, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();

            // Time.Wait(100); // bucket roll
            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t2 = cmd2.ExecuteAsync();

            // Time.Wait(100); // bucket roll
            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t3 = cmd3.ExecuteAsync();

            // Time.Wait(100); // bucket roll
            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t4 = cmd4.ExecuteAsync();

            Task.WaitAll(t1, t2, t3, t4);
            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Assert.Equal(2, stream.LatestRollingMax);
        }

        // BUCKETS
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-F");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-F");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-F");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);
            var cmd4 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);

            Task t1 = cmd1.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t2 = cmd2.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t3 = cmd3.ExecuteAsync();

            WaitForLatchedObserverToUpdate(observer, 1, 100, 125, output);
            Task t4 = cmd4.ExecuteAsync();

            Task.WaitAll(t1, t2, t3, t4);

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutResponseFromCache()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-G");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-G");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-G");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 40);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd4 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Execute();
            cmd2.Execute();
            cmd3.Execute();
            cmd4.Execute();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.True(cmd2.IsResponseFromCache);
            Assert.True(cmd3.IsResponseFromCache);
            Assert.True(cmd4.IsResponseFromCache);
            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutShortCircuits()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-H");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-H");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-H");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // after 3 failures, next command should short-circuit.
            // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
            // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
            var failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE);
            var failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE);
            var failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE);

            var shortCircuited = new List<Command>();

            for (var i = 0; i < 20; i++)
            {
                shortCircuited.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 0));
            }

            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "Health count stream update took to long");

            var shorts = new List<Task<int>>();
            foreach (var cmd in shortCircuited)
            {
                 shorts.Add(cmd.ExecuteAsync());
            }

            Task.WaitAll(shorts.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            foreach (var cmd in shortCircuited)
            {
                Assert.True(cmd.IsResponseShortCircuited);
            }

            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-I");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-I");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-I");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently on different caller threads should saturate semaphore
            // once these are in-flight, execute 10 more concurrently on new caller threads.
            // since these are semaphore-rejected, the max concurrency should be 10
            var saturators = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var rejected = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var tasks = new List<Task>();
            foreach (var saturatingCmd in saturators)
            {
                tasks.Add(Task.Run(() => saturatingCmd.Execute()));
            }

            await Task.Delay(50);

            foreach (var rejectedCmd in rejected)
            {
                await Task.Run(() => rejectedCmd.Execute());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            foreach (var rejectedCmd in rejected)
            {
                Assert.True(rejectedCmd.IsResponseSemaphoreRejected || rejectedCmd.IsResponseShortCircuited);
            }

            // should be 0 since all are executed in a semaphore
            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutThreadPoolRejections()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-J");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-J");
            var key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-J");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently should saturate the Hystrix threadpool
            // once these are in-flight, execute 10 more concurrently
            // since these are threadpool-rejected, the max concurrency should be 10
            var saturators = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 400));
            }

            var rejected = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 100));
            }

            var tasks = new List<Task>();
            foreach (var saturatingCmd in saturators)
            {
                tasks.Add(saturatingCmd.ExecuteAsync());
            }

            Time.Wait(30);

            foreach (var rejectedCmd in rejected)
            {
                rejectedCmd.Observe();
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            foreach (var rejectedCmd in rejected)
            {
                Assert.True(rejectedCmd.IsResponseThreadPoolRejected);
            }

            // this should not count rejected commands
            Assert.Equal(10, stream.LatestRollingMax);
        }
    }
}
