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
    public class RollingThreadPoolEventCounterStreamTest : CommandStreamTest, IDisposable
    {
        private RollingThreadPoolEventCounterStream stream;
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

            protected override void OnNextCore(long[] eventCounts)
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + eventCounts[0] + " : " + eventCounts[1]);
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
            base.Dispose();
            stream.Unsubscribe();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-A");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-A");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-A");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // no writes
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED) + stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleSuccess()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-B");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-B");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-B");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);

            cmd.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-C");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-C");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-C");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            cmd.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleTimeout()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-D");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-D");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-D");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.TIMEOUT);

            cmd.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleBadRequest()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-E");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-E");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-E");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

            cmd.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestFromCache()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-F");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-F");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-F");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Observe();
            cmd2.Observe();
            cmd3.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestShortCircuited()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-G");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-G");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-G");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
            // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
            Command failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);
            Command failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

            Command shortCircuit1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
            Command shortCircuit2 = Command.From(groupKey, key, HystrixEventType.SUCCESS);

            failure1.Observe();
            failure2.Observe();
            failure3.Observe();

            Time.Wait(150);

            shortCircuit1.Observe();
            shortCircuit2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(shortCircuit1.IsResponseShortCircuited);
            Assert.True(shortCircuit2.IsResponseShortCircuited);

            // only the FAILUREs should show up in thread pool counters
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(3, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSemaphoreRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-H");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-H");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-H");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

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
                Task t = new Task(
                () =>
                {
                    saturator.Observe();
                }, CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();
            }

            try
            {
                Time.Wait(100);
            }
            catch (Exception ie)
            {
                Assert.False(true, ie.Message);
            }

            rejected1.Observe();
            rejected2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseSemaphoreRejected);
            Assert.True(rejected2.IsResponseSemaphoreRejected);

            // none of these got executed on a thread-pool, so thread pool metrics should be 0
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestThreadPoolRejected()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-I");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-I");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-I");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            // 10 commands will saturate threadpools when called concurrently.
            // submit 2 more requests and they should be THREADPOOL_REJECTED
            // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
            List<Command> saturators = new List<Command>();

            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 200));
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
                Assert.False(true, ie.Message);
            }

            rejected1.Observe();
            rejected2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(rejected1.IsResponseThreadPoolRejected, "Command1 IsResponseThreadPoolRejected");
            Assert.True(rejected2.IsResponseThreadPoolRejected, "Command2 IsResponseThreadPoolRejected");

            // none of these got executed on a thread-pool, so thread pool metrics should be 0
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(10, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(2, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestFallbackFailure()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-J");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-J");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-J");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_FAILURE);

            cmd.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestFallbackMissing()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-K");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-K");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-K");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(new LatchedObserver(output, latch));

            Command cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20, HystrixEventType.FALLBACK_MISSING);

            cmd.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(1, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestFallbackRejection()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-L");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-L");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-L");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
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
            catch (Exception ie)
            {
                Assert.False(true, ie.Message);
            }

            rejection1.Observe();
            rejection2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            // all 7 commands executed on-thread, so should be executed according to thread-pool metrics
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(7, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleEventsOverTimeGetStoredAndAgeOut()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-M");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-M");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-M");
            stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 250);
            stream.StartCachingStreamValuesIfUnstarted();

            // by doing a take(20), we ensure that all rolling counts go back to 0
            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(20).Subscribe(new LatchedObserver(output, latch));

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

            cmd1.Observe();
            cmd2.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            // all commands should have aged out
            Assert.Equal(2, stream.Latest.Length);
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
            Assert.Equal(0, stream.GetLatestCount(ThreadPoolEventType.REJECTED));
        }
    }
}
