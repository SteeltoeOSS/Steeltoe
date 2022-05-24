// Licensed to the .NET Foundation under one or more agreements.
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
    public class RollingCommandEventCounterStreamTest : CommandStreamTest
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("RollingCommandCounter");
        private readonly ITestOutputHelper output;
        private RollingCommandEventCounterStream stream;
        private IDisposable latchSubscription;

        private sealed class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public RollingCommandEventCounterStreamTest(ITestOutputHelper output)
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
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-A");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.False(HasData(stream.Latest), "Stream has events when it should not");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleSuccess()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-B");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleFailure()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-C");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20);

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleTimeout()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-D");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.TIMEOUT] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.TIMEOUT);
            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleBadRequest()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-E");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST);

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.BAD_REQUEST] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestRequestFromCache()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-F");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.RESPONSE_FROM_CACHE] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public async Task TestShortCircuited()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-G");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            var failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            var failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            var failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);

            var shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);
            var shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "health count took to long to update");

            output.WriteLine(Time.CurrentTimeMillis + " running failures");

            await shortCircuit1.Observe();
            await shortCircuit2.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(shortCircuit1.IsResponseShortCircuited, "Circuit 1 not shorted as was expected");
            Assert.True(shortCircuit2.IsResponseShortCircuited, "Circuit 2 not shorted as was expected");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);

            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 3;
            expected[(int)HystrixEventType.SHORT_CIRCUITED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSemaphoreRejected()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-H");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            var saturators = new List<Command>();

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            var rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            var tasks = new List<Task>();
            foreach (var saturator in saturators)
            {
                tasks.Add(Task.Run(() => saturator.Execute()));
            }

            await Task.Delay(50);

            await Task.Run(() => rejected1.Execute());
            await Task.Run(() => rejected2.Execute());

            Task.WaitAll(tasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (1)");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (2)");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);

            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.SEMAPHORE_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestThreadPoolRejected()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-I");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            var saturators = new List<Command>();

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500));
            }

            var rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            var rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);

            var tasks = new List<Task>();
            foreach (var saturator in saturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            await rejected1.Observe();
            await rejected2.Observe();

            Task.WaitAll(tasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (1)");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (2)");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);

            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.THREAD_POOL_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackFailure()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-J");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_FAILURE);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_FAILURE] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackMissing()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-K");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_MISSING] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackRejection()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-L");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            var fallbackSaturators = new List<Command>();
            for (var i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            var rejection1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            var rejection2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            var tasks = new List<Task>();
            foreach (var saturator in fallbackSaturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            await Task.Delay(50);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 7;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            expected[(int)HystrixEventType.FALLBACK_REJECTION] = 2;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestCollapsed()
        {
            var key = HystrixCommandKeyDefault.AsKey("BatchCommand");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var tasks = new List<Task>();
            for (var i = 0; i < 3; i++)
            {
                tasks.Add(Collapser.From(output, i).ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.COLLAPSED] = 3;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-M");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Take(30 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 10);

            await cmd1.Observe();
            await cmd2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            var expected = new long[HystrixEventTypeHelper.Values.Count];
            Assert.Equal(expected, stream.Latest);
        }
    }
}
