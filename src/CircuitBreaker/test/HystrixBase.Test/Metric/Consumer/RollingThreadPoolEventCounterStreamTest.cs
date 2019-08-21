﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
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
    public class RollingThreadPoolEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private RollingThreadPoolEventCounterStream stream;
        private IDisposable latchSubscription;
        private ITestOutputHelper output;

        private class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public RollingThreadPoolEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            HystrixThreadPoolCompletionStream.Reset();
            RollingThreadPoolEventCounterStream.Reset();
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
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-A");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED) + stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleSuccess()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-B");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-B");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-C");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-C");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-C");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleTimeout()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-D");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-D");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-D");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.TIMEOUT);

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleBadRequest()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-E");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-E");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-E");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestRequestFromCache()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-F");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-F");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-F");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            Command cmd2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestShortCircuited()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-G");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-G");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-G");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
            Command failure1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
            Command failure2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
            Command failure3 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0);

            Command shortCircuit1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);

            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "Health count stream update took to long");

            await shortCircuit1.Observe();
            await shortCircuit2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.True(shortCircuit1.IsResponseShortCircuited);
            Assert.True(shortCircuit2.IsResponseShortCircuited);

            // only the FAILUREs should show up in thread pool counters
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(3, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSemaphoreRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-H");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-H");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-H");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            Command rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            List<Task> tasks = new List<Task>();
            foreach (Command saturator in saturators)
            {
                tasks.Add(Task.Run(() => saturator.Execute()));
            }

            await Task.Delay(50);

            await Task.Run(() => rejected1.Execute());
            await Task.Run(() => rejected2.Execute());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");

            // none of these got executed on a thread-pool, so thread pool metrics should be 0
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestThreadPoolRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-I");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-I");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-I");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command rejected1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            Command rejected2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

            List<Task> tasks = new List<Task>();
            foreach (Command saturator in saturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            await Task.Delay(50);

            await rejected1.Observe();
            await rejected2.Observe();

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseThreadPoolRejected, "Command1 IsResponseThreadPoolRejected");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "Command2 IsResponseThreadPoolRejected");

            // none of these got executed on a thread-pool, so thread pool metrics should be 0
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(10, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-J");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-J");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-J");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_FAILURE);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackMissing()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-K");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-K");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-K");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_MISSING);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackRejection()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-L");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-L");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-L");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<Command> fallbackSaturators = new List<Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            Command rejection1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            Command rejection2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            List<Task> tasks = new List<Task>();
            foreach (Command saturator in fallbackSaturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            await Task.Delay(50);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(7, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-M");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-M");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-M");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 250);
            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Command cmd1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

            await cmd1.Observe();
            await cmd2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // all commands should have aged out
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }
    }
}
