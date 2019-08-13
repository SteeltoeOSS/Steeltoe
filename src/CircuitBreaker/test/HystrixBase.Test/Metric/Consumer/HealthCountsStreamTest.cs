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

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class HealthCountsStreamTest : CommandStreamTest, IDisposable
    {
        private static IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("HealthCounts");
        private HealthCountsStream stream;
        private ITestOutputHelper output;

        private class LatchedObserver : ObserverBase<HealthCounts>
        {
            private CountdownEvent latch;
            private ITestOutputHelper output;

            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            {
                this.latch = latch;
                this.output = output;
            }

            protected override void OnCompletedCore()
            {
                output.WriteLine("OnCompleted @ " + Time.CurrentTimeMillis);
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(HealthCounts healthCounts)
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + healthCounts);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }
        }

        public HealthCountsStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();

            stream.Unsubscribe();
            HealthCountsStream.Reset();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-A");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(0L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleSuccess()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-B");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);

            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-C");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        public async void TestSingleTimeout()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-D");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.TIMEOUT);

            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleBadRequest()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-E");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(0L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestRequestFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-F");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests); // responses from cache should not show up here
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestShortCircuited()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-G");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            Command failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            Command shortCircuit1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(groupKey, key, HystrixEventType.SUCCESS);

            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();
            Time.Wait(500);

            output.WriteLine(Time.CurrentTimeMillis + " Running short circuits");
            await shortCircuit1.Observe();
            await shortCircuit2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.True(shortCircuit1.IsResponseShortCircuited);
            Assert.True(shortCircuit2.IsResponseShortCircuited);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // should only see failures here, not SHORT-CIRCUITS
            Assert.Equal(3L, stream.Latest.ErrorCount);
            Assert.Equal(3L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSemaphoreRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-H");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            foreach (Command saturator in saturators)
            {
                _ = Task.Run(() => saturator.Execute());
            }

            await Task.Delay(50);

            await Task.Run(() => rejected1.Execute());
            await Task.Run(() => rejected2.Execute());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");

            // should only see failures here, not SHORT-CIRCUITS
            Assert.Equal(2L, stream.Latest.ErrorCount);
            Assert.Equal(12L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestThreadPoolRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-I");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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
            Task t = null;
            foreach (Command saturator in saturators)
            {
                t = saturator.ExecuteAsync();
            }

            await rejected1.Observe();
            await rejected2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseThreadPoolRejected, "rejected1 not rejected");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "rejected2 not rejected");
            Assert.Equal(2L, stream.Latest.ErrorCount);
            Assert.Equal(12L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-J");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackMissing()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-K");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1L, stream.Latest.ErrorCount);
            Assert.Equal(1L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackRejection()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-L");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<Command> fallbackSaturators = new List<Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            Command rejection1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            Command rejection2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            foreach (Command saturator in fallbackSaturators)
            {
                _ = saturator.ExecuteAsync();
            }

            await Task.Delay(50);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(7L, stream.Latest.ErrorCount);
            Assert.Equal(7L, stream.Latest.TotalRequests);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-M");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // by doing a take(30), we ensure that all rolling counts go back to 0
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

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
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-N");
            stream = HealthCountsStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            AtomicBoolean allEqual = new AtomicBoolean(false);

            IObservable<HealthCounts> o1 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default);

            IObservable<HealthCounts> o2 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default);

            IObservable<bool> zipped = Observable.Zip(o1, o2, (healthCounts, healthCounts2) =>
                    {
                        return healthCounts == healthCounts2;  // we want object equality
                    });
            IObservable<bool> reduced = zipped.Aggregate(true, (a, b) =>
                    {
                        return a && b;
                    }).Select(n => n);
            var rdisp = reduced.Subscribe(
                (b) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnNext : " + b);
                    allEqual.Value = b;
                },
                (e) =>
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

            for (int i = 0; i < 10; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
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
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Health-O");
            stream = HealthCountsStream.GetInstance(key, 10, 100);

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);
            AtomicInteger healthCounts1 = new AtomicInteger(0);
            AtomicInteger healthCounts2 = new AtomicInteger(0);

            IDisposable s1 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Finally(() =>
                    {
                        latch1.SignalEx();
                    })
                    .Subscribe(
                    (healthCounts) =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnNext : " + healthCounts);
                        healthCounts1.IncrementAndGet();
                    },
                    (e) =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnError : " + e);
                        latch1.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnCompleted");
                        latch1.SignalEx();
                    });
            IDisposable s2 = stream
                    .Observe()
                    .Take(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Finally(() =>
                    {
                        latch2.SignalEx();
                    })
                    .Subscribe(
                        (healthCounts) =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnNext : " + healthCounts + " : " + healthCounts2.Value);
                            healthCounts2.IncrementAndGet();
                        },
                        (e) =>
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
            for (int i = 0; i < 10; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
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
            Assert.True(healthCounts1.Value > 0);
            Assert.True(healthCounts2.Value > 0);
            Assert.True(healthCounts2.Value > healthCounts1.Value);

            s2.Dispose();
        }
    }
}
