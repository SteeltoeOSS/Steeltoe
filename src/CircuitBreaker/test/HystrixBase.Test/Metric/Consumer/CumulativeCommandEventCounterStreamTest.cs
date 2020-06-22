﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
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
    public class CumulativeCommandEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CumulativeCommandCounter");
        private readonly ITestOutputHelper output;
        private CumulativeCommandEventCounterStream stream;
        private IDisposable latchSubscription;

        private class LatchedObserver : TestObserverBase<long[]>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public CumulativeCommandEventCounterStreamTest(ITestOutputHelper output)
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
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-A");

            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.False(HasData(stream.Latest));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleSuccess()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-B");

            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);  // Stream should start
            Command cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)(int)HystrixEventType.SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-C");

            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleTimeout()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-D");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd = Command.From(GroupKey, key, HystrixEventType.TIMEOUT);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.TIMEOUT] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleBadRequest()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-E");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.BAD_REQUEST] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestRequestFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-F");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.RESPONSE_FROM_CACHE] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestShortCircuited()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-G");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

            Command failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            Assert.True(WaitForHealthCountToUpdate(key.Name, 500, output), "health count took to long to update");

            output.WriteLine(Time.CurrentTimeMillis + " running failures");
            shortCircuit1.Execute();
            shortCircuit2.Execute();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");
            Assert.True(shortCircuit1.IsResponseShortCircuited);
            Assert.True(shortCircuit2.IsResponseShortCircuited);
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 3;
            expected[(int)HystrixEventType.SHORT_CIRCUITED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSemaphoreRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-H");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            List<Command> saturators = new List<Command>();
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            Command rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 10 commands will saturate semaphore when called From different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<Task> tasks = new List<Task>();
            foreach (Command c in saturators)
            {
                tasks.Add(Task.Run(() => c.Execute()));
            }

            await Task.Delay(50);

            tasks.Add(Task.Run(() => rejected1.Execute()));
            tasks.Add(Task.Run(() => rejected2.Execute()));

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.SEMAPHORE_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestThreadPoolRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-I");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            List<Command> saturators = new List<Command>();
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            Command rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Task> tasks = new List<Task>();
            foreach (Command saturator in saturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            tasks.Add(rejected1.ExecuteAsync());
            tasks.Add(rejected2.ExecuteAsync());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseThreadPoolRejected);
            Assert.True(rejected2.IsResponseThreadPoolRejected);
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.THREAD_POOL_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-J");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_FAILURE);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_FAILURE] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackMissing()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-K");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_MISSING);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_MISSING] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackRejection()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-L");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            List<Command> fallbackSaturators = new List<Command>();

            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            Command rejection1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            Command rejection2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
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

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 7;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            expected[(int)HystrixEventType.FALLBACK_REJECTION] = 2;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestCancelled()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-M");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            Command toCancel = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500);

            latchSubscription = stream.Observe().Take(5 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : about to Observe and Subscribe");
            IDisposable s = toCancel.Observe().
                    OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : UnSubscribe From command.Observe()");
                    }).Subscribe(
                    (i) =>
                    {
                        output.WriteLine("Command OnNext : " + i);
                    },
                    (e) =>
                    {
                        output.WriteLine("Command OnError : " + e);
                    },
                    () =>
                    {
                        output.WriteLine("Command OnCompleted");
                    });

            output.WriteLine(Time.CurrentTimeMillis + " : " + Task.CurrentId + " : about to unSubscribe");
            s.Dispose();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.CANCELLED] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestCollapsed()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(Collapser.From(output, i).ExecuteAsync());
            }

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, output), "Latch took to long to update");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.COLLAPSED] = 3;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleEventsOverTimeGetStoredAndNeverAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-N");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 100);
            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 10);

            // by doing a Take(30), we ensure that no rolling out of window takes place
            latchSubscription = stream.Observe().Take(30 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }
    }
}
