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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCommandMaxConcurrencyStreamTest : CommandStreamTest
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Command-Concurrency");
        private RollingCommandMaxConcurrencyStream stream;
        private IDisposable latchSubscription;

        private sealed class LatchedObserver : TestObserverBase<int>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        private readonly ITestOutputHelper output;

        public RollingCommandMaxConcurrencyStreamTest(ITestOutputHelper output)
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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-A");
            var latch = new CountdownEvent(1);
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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);

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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-C");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 250);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-D");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            var cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

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
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-E");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            var cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-F");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 40);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd4 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-G");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // after 3 failures, next command should short-circuit.
            // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
            // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
            var failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            var failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            var failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE);

            var shortCircuited = new List<Command>();

            for (var i = 0; i < 20; i++)
            {
                shortCircuited.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0));
            }

            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "Health count stream update took to long");

            var tasks = new List<Task>();
            foreach (var cmd in shortCircuited)
            {
                tasks.Add(cmd.ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-H");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently on different caller threads should saturate semaphore
            // once these are in-flight, execute 10 more concurrently on new caller threads.
            // since these are semaphore-rejected, the max concurrency should be 10
            var saturators = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var rejected = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var sattasks = new List<Task>();
            foreach (var saturatingCmd in saturators)
            {
                sattasks.Add(Task.Run(() => saturatingCmd.Execute()));
            }

            await Task.Delay(50);

            foreach (var rejectedCmd in rejected)
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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-I");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently should saturate the Hystrix threadpool
            // once these are in-flight, execute 10 more concurrently
            // since these are threadpool-rejected, the max concurrency should be 10
            var saturators = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400));
            }

            var rejected = new List<Command>();
            for (var i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100));
            }

            var tasks = new List<Task>();
            foreach (var saturatingCmd in saturators)
            {
                tasks.Add(saturatingCmd.ExecuteAsync());
            }

            Time.Wait(30);

            foreach (var rejectedCmd in rejected)
            {
                tasks.Add(rejectedCmd.ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(10, stream.LatestRollingMax);
        }
    }
}
