﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
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
    public class RollingCommandLatencyDistributionStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CommandLatency");
        private RollingCommandLatencyDistributionStream stream;
        private IDisposable latchSubscription;
        private ITestOutputHelper output;

        private class LatchedObserver : TestObserverBase<CachedValuesHistogram>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public RollingCommandLatencyDistributionStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            RollingCommandLatencyDistributionStream.Reset();
            HystrixCommandCompletionStream.Reset();
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
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-A");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        [Fact]
        public async void TestSingleBucketGetsStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            await cmd1.Observe();
            await cmd2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            AssertBetween(100, 400, stream.LatestMean);
            AssertBetween(10, 100, stream.GetLatestPercentile(0.0));
            AssertBetween(300, 800, stream.GetLatestPercentile(100.0));
        }

        // The following event types should not have their latency measured:
        // THREAD_POOL_REJECTED
        // SEMAPHORE_REJECTED
        // SHORT_CIRCUITED
        // RESPONSE_FROM_CACHE
        // Newly measured (as of 1.5)
        // BAD_REQUEST
        // FAILURE
        // TIMEOUT
        [Fact]
        public async void TestSingleBucketWithMultipleEventTypes()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-C");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 30);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST, 40);

            await cmd1.Observe();
            await cmd3.Observe();
            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd4.Observe());
            await cmd2.Observe();  // Timeout should run last

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            AssertBetween(100, 400, stream.LatestMean); // now timeout latency of 600ms is there
            AssertBetween(10, 100, stream.GetLatestPercentile(0.0));
            AssertBetween(300, 800, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        public async void TestShortCircuitedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-D");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 3 failures is enough to trigger short-circuit.  execute those, then wait for bucket to roll
            // next command should be a short-circuit
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 3; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0));
            }

            Command shortCircuit = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            foreach (Command cmd in commands)
            {
                await cmd.Observe();
            }

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "health count took to long to update");

            try
            {
                await shortCircuit.Observe();
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(3, stream.Latest.GetTotalCount());
            AssertBetween(0, 75, stream.LatestMean);

            Assert.True(shortCircuit.IsResponseShortCircuited);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestThreadPoolRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-E");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands with latency should occupy the entire threadpool.  execute those, then wait for bucket to roll
            // next command should be a thread-pool rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command threadPoolRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            List<Task> satTasks = new List<Task>();
            foreach (Command cmd in commands)
            {
                satTasks.Add(cmd.ExecuteAsync());
            }

            await threadPoolRejected.Observe();
            Task.WaitAll(satTasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(500, 750, stream.LatestMean);
            Assert.True(threadPoolRejected.IsResponseThreadPoolRejected);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSemaphoreRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-F");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands with latency should occupy all semaphores.  execute those, then wait for bucket to roll
            // next command should be a semaphore rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command semaphoreRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            List<Task> satTasks = new List<Task>();
            foreach (Command saturator in commands)
            {
                satTasks.Add(Task.Run(() => saturator.Execute()));
            }

            await Task.Delay(50);

            await Task.Run(() => semaphoreRejected.Execute());

            Task.WaitAll(satTasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(500, 750, stream.LatestMean);
            Assert.True(semaphoreRejected.IsResponseSemaphoreRejected);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestResponseFromCacheDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-G");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // should get 1 SUCCESS and 1 RESPONSE_FROM_CACHE
            List<Command> commands = Command.GetCommandsWithResponseFromCache(GroupKey, key);

            foreach (Command cmd in commands)
            {
                _ = cmd.ExecuteAsync();
            }

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(1, stream.Latest.GetTotalCount());
            AssertBetween(0, 75, stream.LatestMean);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleBucketsBothGetStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-H");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            await cmd1.Observe();
            await cmd2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            await cmd3.Observe();
            await cmd4.Observe();
            await cmd5.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            AssertBetween(50, 150, stream.LatestMean);
            AssertBetween(10, 150, stream.GetLatestPercentile(0.0));
            AssertBetween(100, 150, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        public async void TestMultipleBucketsBothGetStoredAndThenAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-I");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            await cmd1.Observe();
            await cmd2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            await cmd3.Observe();
            await cmd4.Observe();
            await cmd5.Observe();

            WaitForLatchedObserverToUpdate(observer, 1, 500, output);

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        private void AssertBetween(int expectedLow, int expectedHigh, int value)
        {
            output.WriteLine("Low:" + expectedLow + " High:" + expectedHigh + " Value: " + value);
            Assert.InRange(value, expectedLow, expectedHigh);
        }
    }
}
