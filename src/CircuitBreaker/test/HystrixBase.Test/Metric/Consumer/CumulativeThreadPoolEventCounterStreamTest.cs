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
    public class CumulativeThreadPoolEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private CumulativeThreadPoolEventCounterStream stream;
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
                output.WriteLine("OnCompletedCore @ " + (DateTime.Now.Ticks / 10000) + " " + Thread.CurrentThread.ManagedThreadId);
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(long[] eventCounts)
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + eventCounts[0] + " : " + eventCounts[1] + " " + Thread.CurrentThread.ManagedThreadId);
            }
        }

        public CumulativeThreadPoolEventCounterStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();

            stream.Unsubscribe();
            CumulativeThreadPoolEventCounterStream.Reset();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-A");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-A");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-A");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // no writes
            try
            {
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestSingleSuccess()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-B");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-B");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-B");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestSingleFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-C");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-C");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-C");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }

        [Fact]
        public void TestSingleTimeout()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-D");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-D");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-D");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestSingleBadRequest()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-E");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-E");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-E");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestRequestFromCache()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-F");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-F");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-F");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void TestShortCircuited()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-G");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-G");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-G");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
            Command failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            Command shortCircuit1 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = CommandStreamTest.Command.From(groupKey, key, HystrixEventType.SUCCESS);

            failure1.Observe();
            failure2.Observe();
            failure3.Observe();

            Time.Wait(150);

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

            // only the FAILUREs should show up in thread pool counters
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(3, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestSemaphoreRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-H");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-H");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-H");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate semaphore when called from different threads.
            // submit 2 more requests and they should be SEMAPHORE_REJECTED
            // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 300, ExecutionIsolationStrategy.SEMAPHORE));
            }

            Command rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
            Command rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

            foreach (Command c in saturators)
            {
                Task t = new Task(
                () =>
                {
                    c.Observe();
                },  CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();

                Task.Run(() => c.Observe());
            }

            Time.Wait(10);

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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestThreadPoolRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-I");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-I");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-I");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 700));
            }

            Command rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
            Command rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

            foreach (Command c in saturators)
            {
                c.Observe();
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

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(10, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestFallbackFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-J");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-J");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-J");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestFallbackMissing()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-K");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-K");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-K");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        public void TestFallbackRejection()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-L");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-L");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-L");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(new LatchedObserver(output, latch));

            // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
            // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
            List<Command> fallbackSaturators = new List<Command>();
            for (int i = 0; i < 5; i++)
            {
                fallbackSaturators.Add(CommandStreamTest.Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_SUCCESS, 400));
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
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // all 7 commands executed on-thread, so should be executed according to thread-pool metrics
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(7, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        // in a rolling window, take(30) would age out all counters.  in the cumulative count, we expect them to remain non-zero forever
        [Fact]
        public void TestMultipleEventsOverTimeGetStoredAndDoNotAgeOut()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Cumulative-ThreadPool-M");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("Cumulative-ThreadPool-M");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cumulative-Counter-M");
            stream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // all commands should have aged out
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }
    }
}
