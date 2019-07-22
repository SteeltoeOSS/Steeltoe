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
    public class CumulativeCommandEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private static IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("CumulativeCommandCounter");
        private CumulativeCommandEventCounterStream stream;
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
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] value)
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + BucketToString(value));
            }
        }

        public CumulativeCommandEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();

            stream.Unsubscribe();
            CumulativeCommandEventCounterStream.Reset();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-A");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // no writes
            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.False(true, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.False(HasData(stream.Latest));
        }

        [Fact]
        public void TestSingleSuccess()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-B");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)(int)HystrixEventType.SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-C");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleTimeout()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-D");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.TIMEOUT);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.TIMEOUT] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleBadRequest()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-E");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.BAD_REQUEST] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestRequestFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-F");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Observe();
            cmd2.Observe();
            cmd3.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.RESPONSE_FROM_CACHE] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestShortCircuited()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-G");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            Command failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            Command shortCircuit1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(groupKey, key, HystrixEventType.SUCCESS);

            failure1.Observe();
            failure2.Observe();
            failure3.Observe();

            try
            {
                Time.Wait(500);
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            shortCircuit1.Observe();
            shortCircuit2.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
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
        public void TestSemaphoreRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-H");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate semaphore when called From different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            Command rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            foreach (Command c in saturators)
            {
                Task t = new Task(
                () =>
                {
                    c.Observe();
                }, CancellationToken.None,
                   TaskCreationOptions.LongRunning);
                t.Start();
            }

            Time.Wait(100);

            Task.Run(() => rejected1.Observe());
            Task.Run(() => rejected2.Observe());

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseSemaphoreRejected);
            Assert.True(rejected2.IsResponseSemaphoreRejected);
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 10;
            expected[(int)HystrixEventType.SEMAPHORE_REJECTED] = 2;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestThreadPoolRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-I");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500));
            }

            Command rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            Command rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

            foreach (Command saturator in saturators)
            {
                saturator.Observe();
            }

            try
            {
                Time.Wait(100);
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            rejected1.Observe();
            rejected2.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
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
        public void TestFallbackFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-J");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_FAILURE] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestFallbackMissing()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-K");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_MISSING] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestFallbackRejection()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-L");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<Command> fallbackSaturators = new List<Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 400));
            }

            Command rejection1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 0);
            Command rejection2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 0);

            foreach (Command saturator in fallbackSaturators)
            {
                saturator.Observe();
            }

            try
            {
                Time.Wait(70);
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }

            output.WriteLine("ReqLog1 : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            rejection1.Observe();
            rejection2.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog2 : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 7;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            expected[(int)HystrixEventType.FALLBACK_REJECTION] = 2;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 2;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestCancelled()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-M");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command toCancel = Command.From(groupKey, key, HystrixEventType.SUCCESS, 500);

            output.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " : about to Observe and Subscribe");
            IDisposable s = toCancel.Observe().
                    OnDispose(() =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Thread.CurrentThread.ManagedThreadId + " : UnSubscribe From command.Observe()");
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

            output.WriteLine((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + " : " + Task.CurrentId + " : about to unSubscribe");
            s.Dispose();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.CANCELLED] = 1;
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestCollapsed()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            for (int i = 0; i < 3; i++)
            {
                CommandStreamTest.Collapser.From(output, i).Observe();
            }

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            expected[(int)HystrixEventType.COLLAPSED] = 3;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(expected, stream.Latest);
        }

        [Fact]
        public void TestMultipleEventsOverTimeGetStoredAndNeverAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-N");
            stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // by doing a Take(30), we ensure that no rolling out of window takes place
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

            cmd1.Observe();
            cmd2.Observe();

            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

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
