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
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingThreadPoolMaxConcurrencyStreamTest : CommandStreamTest, IDisposable
    {
        private RollingThreadPoolMaxConcurrencyStream stream;
        private IDisposable latchSubscription;
        private ITestOutputHelper output;

        private class LatchedObserver : ObserverBase<int>
        {
            private CountdownEvent latch;
            private ITestOutputHelper output;

            public bool StreamRunning { get; set; } = false;

            public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            {
                this.latch = latch;
                this.output = output;
            }

            protected override void OnCompletedCore()
            {
                StreamRunning = false;
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(int maxConcurrency)
            {
                StreamRunning = true;
                output?.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : Max of " + maxConcurrency);
                output?.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }
        }

        public RollingThreadPoolMaxConcurrencyStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;

            HystrixThreadPoolStartStream.Reset();
            RollingThreadPoolMaxConcurrencyStream.Reset();
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
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-A");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestStartsAndEndsInSameBucketProduceValue()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-B");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-B");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-B");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 50);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 40);

            cmd1.Observe();
            cmd2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(2, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestStartsAndEndsInSameBucketSemaphoreIsolated()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-C");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-C");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-C");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10, ExecutionIsolationStrategy.SEMAPHORE);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 14, ExecutionIsolationStrategy.SEMAPHORE);

            cmd1.Observe();
            cmd2.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            // since commands run in semaphore isolation, they are not tracked by threadpool metrics
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, stream.LatestRollingMax);
        }

        /***
         * 3 Commands,
         * Command 1 gets started in Bucket A and not completed until Bucket B
         * Commands 2 and 3 both start and end in Bucket B, and there should be a max-concurrency of 3
         */
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestOneCommandCarriesOverToNextBucket()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-D");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-D");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-D");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 560);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 50);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 75);

            cmd1.Observe();

            // Time.Wait(150); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd2.Observe();
            cmd3.Observe();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //      A    |    B    |    C    |    D    |    E    |
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBuckets()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-E");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-E");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-E");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);

            cmd1.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd2.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd3.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd4.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //      A    |    B    |    C    |    D    |    E    |
        //  1:  [-------------------------------]              ThreadPool x
        //  2:          [-------------------------------]                 y
        //  3:                      [--]                                  x
        //  4:                              [--]                          x
        //  Same input data as above test, just that command 2 runs in a separate threadpool, so concurrency should not get tracked
        //  Max concurrency should be 2 for x
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsForMultipleThreadPools()
        {
            IHystrixCommandGroupKey groupKeyX = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-X");
            IHystrixCommandGroupKey groupKeyY = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-Y");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-X");
            IHystrixCommandKey keyX = HystrixCommandKeyDefault.AsKey("RollingConcurrency-X");
            IHystrixCommandKey keyY = HystrixCommandKeyDefault.AsKey("RollingConcurrency-Y");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKeyX, keyX, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(groupKeyY, keyY, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(groupKeyX, keyY, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(groupKeyX, keyY, HystrixEventType.SUCCESS, 10);

            cmd1.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd2.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd3.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd4.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(2, stream.LatestRollingMax);
        }

        // BUCKETS
        //  1:  [-------------------------------]
        //  2:          [-------------------------------]
        //  3:                      [--]
        //  4:                              [--]
        //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-F");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-F");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-F");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(30).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 10);

            cmd1.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd2.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd3.Observe();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForObservableToUpdate(stream.Observe(), 1, 250, output), "Stream update took to long");
            cmd4.Observe();
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutResponseFromCache()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-G");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-G");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-G");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(5).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            Command cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 40);
            Command cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd4 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Execute();
            cmd2.Execute();
            cmd3.Execute();
            cmd4.Execute();

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(cmd2.IsResponseFromCache);
            Assert.True(cmd3.IsResponseFromCache);
            Assert.True(cmd4.IsResponseFromCache);
            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutShortCircuits()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-H");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-H");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-H");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // after 3 failures, next command should short-circuit.
            // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
            // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
            Command failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE);
            Command failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE);
            Command failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE);

            List<Command> shortCircuited = new List<Command>();

            for (int i = 0; i < 20; i++)
            {
                shortCircuited.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 0));
            }

            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            // Time.Wait(100); // bucket roll
            Assert.True(WaitForHealthCountToUpdate(key.Name, 250, output), "Health count stream update took to long");

            List<Task<int>> shorts = new List<Task<int>>();
            foreach (Command cmd in shortCircuited)
            {
                 shorts.Add(cmd.ExecuteAsync());
            }

            Task.WaitAll(shorts.ToArray());

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

            foreach (Command cmd in shortCircuited)
            {
                Assert.True(cmd.IsResponseShortCircuited);
            }

            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async void TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-I");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-I");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-I");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently on different caller threads should saturate semaphore
            // once these are in-flight, execute 10 more concurrently on new caller threads.
            // since these are semaphore-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE));
            }

            foreach (Command saturatingCmd in saturators)
            {
                _ = Task.Run(() => saturatingCmd.Execute());
            }

            await Task.Delay(50);

            foreach (Command rejectedCmd in rejected)
            {
                await Task.Run(() => rejectedCmd.Execute());
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            foreach (Command rejectedCmd in rejected)
            {
                Assert.True(rejectedCmd.IsResponseSemaphoreRejected || rejectedCmd.IsResponseShortCircuited);
            }

            // should be 0 since all are executed in a semaphore
            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestConcurrencyStreamProperlyFiltersOutThreadPoolRejections()
        {
            IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-J");
            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-J");
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-J");
            CountdownEvent latch = new CountdownEvent(1);
            var observer = new LatchedObserver(output, latch);

            stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
            latchSubscription = stream.Observe().Take(10).Subscribe(observer);
            Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

            // 10 commands executed concurrently should saturate the Hystrix threadpool
            // once these are in-flight, execute 10 more concurrently
            // since these are threadpool-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 400));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 100));
            }

            foreach (Command saturatingCmd in saturators)
            {
                saturatingCmd.Observe();
            }

            Time.Wait(30);

            foreach (Command rejectedCmd in rejected)
            {
                rejectedCmd.Observe();
            }

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            foreach (Command rejectedCmd in rejected)
            {
                Assert.True(rejectedCmd.IsResponseThreadPoolRejected);
            }

            // this should not count rejected commands
            Assert.Equal(10, stream.LatestRollingMax);
        }
    }
}
