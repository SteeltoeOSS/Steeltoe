// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class HealthCountsStreamTest : CommandStreamTest
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("HealthCounts");
        private readonly ITestOutputHelper output;
        private HealthCountsStream stream;
        private IDisposable latchSubscription;

        private sealed class LatchedObserver : TestObserverBase<HealthCounts>
        {
            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
                : base(output, latch)
            {
            }
        }

        public HealthCountsStreamTest(ITestOutputHelper output)
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
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-A");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(0L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleSuccess()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-B");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleFailure()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-C");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        public async Task TestSingleTimeout()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-D");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd = Command.From(GroupKey, key, HystrixEventType.TIMEOUT);  // Timeout 1000
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleBadRequest()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-E");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(0L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestRequestFromCache()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-F");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            var cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();

            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests); // responses from cache should not show up here
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestShortCircuited()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-G");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            var failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            var failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            var shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);
            var shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();

            output.WriteLine(Time.CurrentTimeMillis + " Waiting for health window to change");
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            output.WriteLine(Time.CurrentTimeMillis + " Running short circuits");

            await shortCircuit1.Observe();
            await shortCircuit2.Observe();
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(shortCircuit1.IsResponseShortCircuited);
            Assert.True(shortCircuit2.IsResponseShortCircuited);

            // should only see failures here, not SHORT-CIRCUITS
            Assert.Equal(3L, stream.Latest.ErrorCount);
            Assert.Equal(3L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSemaphoreRejected()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-H");
            var saturators = new List<Command>();

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            var rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            var rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

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
            Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");

            // should only see failures here, not SHORT-CIRCUITS
            Assert.Equal(2L, stream.Latest.ErrorCount);
            Assert.Equal(12L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestThreadPoolRejected()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-I");
            var saturators = new List<Command>();

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            for (var i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400));
            }

            var rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            var rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            var tasks = new List<Task>();
            foreach (var saturator in saturators)
            {
                tasks.Add(saturator.ExecuteAsync());
            }

            tasks.Add(rejected1.ExecuteAsync());
            tasks.Add(rejected2.ExecuteAsync());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.True(rejected1.IsResponseThreadPoolRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "rejected2 not rejected");
            Assert.Equal(2L, stream.Latest.ErrorCount);
            Assert.Equal(12L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackFailure()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-J");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackMissing()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-K");
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);
            latchSubscription = stream.Observe().Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");

            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestFallbackRejection()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-L");
            var fallbackSaturators = new List<Command>();
            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            for (var i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            var rejection1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            var rejection2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

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

            output.WriteLine("ReqLog1 @ " + Time.CurrentTimeMillis + " " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            output.WriteLine("ReqLog2 @ " + Time.CurrentTimeMillis + " " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Task.WaitAll(tasks.ToArray());
            Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, output), "Latch took to long to update");
            Assert.Equal(7L, stream.Latest.ErrorCount);
            Assert.Equal(7L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-M");

            var latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            var cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 10);

            // by doing a take(30), we ensure that all rolling counts go back to 0
            latchSubscription = stream.Observe().Take(30 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            await cmd1.Observe();
            await cmd2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(0L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSharedSourceStream()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-N");

            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var latch = new CountdownEvent(1);
            var allEqual = new AtomicBoolean(false);

            var o1 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default);

            var o2 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default);

            var zipped = o1.Zip(o2, (healthCounts, healthCounts2) => healthCounts == healthCounts2);
            var reduced = zipped.Aggregate(true, (a, b) => a && b).Select(n => n);

            var rdisp = reduced.Subscribe(
                b =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnNext : " + b);
                    allEqual.Value = b;
                },
                e =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnError : " + e);
                    output.WriteLine(e.ToString());
                    latch.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnCompleted");
                    latch.SignalEx();
                });

            for (var i = 0; i < 10; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
                cmd.Execute();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.True(allEqual.Value);

            rdisp.Dispose();

            // we should be getting the same object from both streams.  this ensures that multiple subscribers don't induce extra work
        }

        [Fact]
        public void TestTwoSubscribersOneUnsubscribes()
        {
            var key = HystrixCommandKeyDefault.AsKey("CMD-Health-O");
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            var healthCounts1 = new AtomicInteger(0);
            var healthCounts2 = new AtomicInteger(0);

            var s1 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Finally(() =>
                    {
                        latch1.SignalEx();
                    })
                    .Subscribe(
                    healthCounts =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnNext : " + healthCounts);
                        healthCounts1.IncrementAndGet();
                    },
                    e =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnError : " + e);
                        latch1.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnCompleted");
                        latch1.SignalEx();
                    });
            var s2 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Finally(() =>
                    {
                        latch2.SignalEx();
                    })
                    .Subscribe(
                        healthCounts =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnNext : " + healthCounts + " : " + healthCounts2.Value);
                            healthCounts2.IncrementAndGet();
                        },
                        e =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnError : " + e);
                            latch2.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnCompleted");
                            latch2.SignalEx();
                        });

            // execute 5 commands, then unsubscribe from first stream. then execute the rest
            for (var i = 0; i < 10; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
                cmd.Execute();
                if (i == 5)
                {
                    s1.Dispose();
                }
            }

            Assert.True(stream.IsSourceCurrentlySubscribed);  // only 1/2 subscriptions has been cancelled

            Assert.True(latch1.Wait(10000));
            Assert.True(latch2.Wait(10000));
            output.WriteLine("s1 got : " + healthCounts1.Value + ", s2 got : " + healthCounts2.Value);
            Assert.True(healthCounts1.Value >= 0);
            Assert.True(healthCounts2.Value > 0);
            Assert.True(healthCounts2.Value > healthCounts1.Value);

            s2.Dispose();
        }
    }
}
