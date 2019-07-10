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
                output.WriteLine("OnCompletedCore @ " + (DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId);
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] eventCounts)
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + BucketToString(eventCounts) + " " + Thread.CurrentThread.ManagedThreadId);
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
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-A");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            var disposable = stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            // no writes
            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            Assert.False(HasData(stream.Latest));
        }

        [Fact]
        public void TestSingleSuccess()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-B");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-C");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.FAILURE, 20);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleTimeout()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-D");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.TIMEOUT);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.TIMEOUT] = 1;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestSingleBadRequest()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-E");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            Command cmd = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.BAD_REQUEST] = 1;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 1;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestRequestFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-F");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Observe();
            cmd2.Observe();
            cmd3.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
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
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact(Skip = "Fails on hosted agent")]
        public void TestShortCircuited()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-G");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
            Command failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);
            Command failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 0);

            Command shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            failure1.Observe();
            failure2.Observe();
            failure3.Observe();

            try
            {
                Time.Wait(125);
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            shortCircuit1.Observe();
            shortCircuit2.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
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
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestSemaphoreRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-H");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<CommandStreamTest.Command> saturators = new List<CommandStreamTest.Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400, ExecutionIsolationStrategy.SEMAPHORE));
            }

            CommandStreamTest.Command rejected1 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            CommandStreamTest.Command rejected2 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            foreach (Command c in saturators)
            {
                new Thread(new ThreadStart(() => c.Observe())).Start();
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
                Assert.True(latch.Wait(10000));
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
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestThreadPoolRejected()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-I");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<CommandStreamTest.Command> saturators = new List<CommandStreamTest.Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 200));
            }

            CommandStreamTest.Command rejected1 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);
            CommandStreamTest.Command rejected2 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 0);

            foreach (CommandStreamTest.Command saturator in saturators)
            {
                saturator.Observe();
            }

            rejected1.Observe();
            rejected2.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
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
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestFallbackFailure()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-J");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            CommandStreamTest.Command cmd = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
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
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestFallbackMissing()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-K");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            CommandStreamTest.Command cmd = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);

            cmd.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
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
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestFallbackRejection()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-L");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<CommandStreamTest.Command> fallbackSaturators = new List<CommandStreamTest.Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 400));
            }

            CommandStreamTest.Command rejection1 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 0);
            CommandStreamTest.Command rejection2 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 0);

            foreach (CommandStreamTest.Command saturator in fallbackSaturators)
            {
                saturator.Observe();
            }

            try
            {
                Time.Wait(70);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message);
            }

            rejection1.Observe();
            rejection2.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            expected[(int)HystrixEventType.FAILURE] = 7;
            expected[(int)HystrixEventType.FALLBACK_SUCCESS] = 5;
            expected[(int)HystrixEventType.FALLBACK_REJECTION] = 2;
            expected[(int)HystrixEventType.EXCEPTION_THROWN] = 2;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestCollapsed()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(this.output, latch));

            for (int i = 0; i < 3; i++)
            {
                CommandStreamTest.Collapser.From(output, i).Observe();
            }

            try
            {
                Assert.True(latch.Wait(10000));
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
            Assert.Equal<long[]>(expected, stream.Latest);
        }

        [Fact]
        public void TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-M");
            stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // by doing a take(30), we ensure that all rolling counts go back to 0
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(new LatchedObserver(this.output, latch));

            CommandStreamTest.Command cmd1 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.SUCCESS, 20);
            CommandStreamTest.Command cmd2 = CommandStreamTest.Command.From(GroupKey, key, HystrixEventType.FAILURE, 10);

            cmd1.Observe();
            cmd2.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(HystrixEventTypeHelper.Values.Count, stream.Latest.Length);
            long[] expected = new long[HystrixEventTypeHelper.Values.Count];
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal<long[]>(expected, stream.Latest);
        }
    }
}
