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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCommandEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("RollingCommandCounter");
        private RollingCommandEventCounterStream stream;
        private ITestOutputHelper output;

        private class LatchedObserver : ObserverBase<long[]>
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
                output.WriteLine("OnCompletedCore @ " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId);
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] eventCounts)
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + BucketToString(eventCounts) + " " + Thread.CurrentThread.ManagedThreadId);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }
        }

        public RollingCommandEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            RollingCommandEventCounterStream.Reset();
            HystrixCommandCompletionStream.Reset();
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();
            stream.Unsubscribe();
            RollingCommandEventCounterStream.Reset();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-A");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.False(HasData(stream.Latest), "Stream has events when it should not");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleSuccess()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-B");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);

            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-C");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20);

            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleTimeout()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-D");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 600);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.TIMEOUT);
            await cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.TIMEOUT] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSingleBadRequest()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-E");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST);

            await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.BAD_REQUEST] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestRequestFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-F");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            await cmd1.Observe();
            await cmd2.Observe();
            await cmd3.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.RESPONSE_FROM_CACHE] = 2;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public async void TestShortCircuited()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-G");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // var stream2 = HystrixCommandCompletionStream.GetInstance(key);
            // var observable2 = stream2.Observe().Subscribe((comp) =>
            // {
            //    output.WriteLine("CompletionStream: " + Time.CurrentTimeMillis + " " + comp.ToString() + " " + Thread.CurrentThread.ManagedThreadId);
            // });
            // var stream3 = HealthCountsStream.GetInstance(key, 10, 100);
            // var observable3 = stream3.Observe().Subscribe((comp) =>
            // {
            //    output.WriteLine("HealthCountStream: " + Time.CurrentTimeMillis + " " + comp.ToString() + " " + Thread.CurrentThread.ManagedThreadId);
            // });
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            Command failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);

            Command shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            await failure1.Observe();
            await failure2.Observe();
            await failure3.Observe();

            Time.Wait(200);

            output.WriteLine(Time.CurrentTimeMillis + " running failures");

            await shortCircuit1.Observe();
            await shortCircuit2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(shortCircuit1.IsResponseShortCircuited, "Circuit 1 not shorted as was expected");
            Assert.True(shortCircuit2.IsResponseShortCircuited, "Circuit 2 not shorted as was expected");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 3;
            expected[(int)HystrixEventType.SHORT_CIRCUITED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestSemaphoreRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-H");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            Command rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            foreach (Command saturator in saturators)
            {
                _ = Task.Run(() => saturator.Execute());
            }

            await Task.Delay(50);

            await Task.Run(() => rejected1.Execute());
            await Task.Run(() => rejected2.Execute());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (1)");
            Assert.True(rejected2.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (2)");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.SEMAPHORE_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestThreadPoolRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-I");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(7).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command rejected1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            Command rejected2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);

            Task t = null;
            foreach (Command saturator in saturators)
            {
                t = saturator.ExecuteAsync();
            }

            await rejected1.Observe();
            await rejected2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.True(rejected1.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (1)");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (2)");
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
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-J");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_FAILURE] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackMissing()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-K");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_MISSING] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestFallbackRejection()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-L");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<Command> fallbackSaturators = new List<Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
            }

            Command rejection1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
            Command rejection2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

            foreach (Command saturator in fallbackSaturators)
            {
                _ = saturator.ExecuteAsync();
            }

            await Task.Delay(50);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 7;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            expected[(int)HystrixEventType.FALLBACK_REJECTION] = 2;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 2;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestCollapsed()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            for (int i = 0; i < 3; i++)
            {
                Collapser.From(output, i).Observe();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.COLLAPSED] = 3;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-M");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // by doing a take(30), we ensure that all rolling counts go back to 0
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 10);

            await cmd1.Observe();
            await cmd2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }
    }
}
