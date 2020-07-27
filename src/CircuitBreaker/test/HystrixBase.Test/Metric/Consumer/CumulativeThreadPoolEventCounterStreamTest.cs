// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
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
    public class CumulativeThreadPoolEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private readonly ITestOutputHelper output;
        private CumulativeThreadPoolEventCounterStream stream;
        private IDisposable latchSubscription;

        private class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public CumulativeThreadPoolEventCounterStreamTest(ITestOutputHelper output)
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
        public void TestEmptyStreamProducesZeros()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-A");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-A");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-A");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleSuccess()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-B");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-B");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            var cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleFailure()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-C");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-C");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-C");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleTimeout()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-D");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-D");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-D");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            var cmd = Command.From(groupKey, key, HystrixEventType.TIMEOUT);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleBadRequest()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-E");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-E");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-E");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            var cmd = Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestRequestFromCache()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-F");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-F");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-F");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestShortCircuited()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-G");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-G");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-G");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            var failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
            var failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
            var failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);

            var shortCircuit1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);
            var shortCircuit2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "health count took to long to update");

            output.WriteLine(Time.CurrentTimeMillis + " running failures");
            await shortCircuit1.Observe();
            await shortCircuit2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

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
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-H");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-H");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-H");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            var saturators = new List<Command>();

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            var rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            var tasks = new List<Task>();
            foreach (var saturator in saturators)
            {
                tasks.Add(Task.Run(() => saturator.Execute()));
            }

            await Task.Delay(50);

            tasks.Add(Task.Run(() => rejected1.Execute()));
            tasks.Add(Task.Run(() => rejected2.Execute()));

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseSemaphoreRejected);
            Assert.True(rejected2.IsResponseSemaphoreRejected);

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestThreadPoolRejected()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-I");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-I");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-I");

            var saturators = new List<Command>();
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500));
            }

            var rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            var rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            var tasks = new List<Task>();
            foreach (var c in saturators)
            {
                tasks.Add(c.ExecuteAsync());
            }

            Time.Wait(50);

            tasks.Add(rejected1.ExecuteAsync());
            tasks.Add(rejected2.ExecuteAsync());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseThreadPoolRejected);
            Assert.True(rejected2.IsResponseThreadPoolRejected);

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(10, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackFailure()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-J");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-J");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-J");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_FAILURE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackMissing()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-K");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-K");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-K");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);

            var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_MISSING);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackRejection()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-L");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-L");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-L");

            var fallbackSaturators = new List<Command>();
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            for (var i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            var rejection1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            var rejection2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            var tasks = new List<Task>();
            foreach (var saturator in fallbackSaturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            await Task.Delay(50);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            // all 7 commands executed on-thread, so should be executed according to thread-pool metrics
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(7, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        // in a rolling window, take(20) would age out all counters.  in the cumulative count, we expect them to remain non-zero forever
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleEventsOverTimeGetStoredAndDoNotAgeOut()
        {
            var groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-M");
            var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-M");
            var key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-M");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            var cmd2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

            latchSubscription = stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();
            Assert.True(latch.Wait(20000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // all commands should not have aged out
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }
    }
}
