// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
    public class RollingCommandMaxConcurrencyStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Command-Concurrency");
        private RollingCommandMaxConcurrencyStream stream;

        private class LatchedObserver : ObserverBase<int>
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

            protected override void OnNextCore(int value)
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : Max of " + value);
            }
        }

        private static LatchedObserver GetSubscriber(ITestOutputHelper output, CountdownEvent latch)
        {
            return new LatchedObserver(output, latch);
        }

        private ITestOutputHelper output;

        public RollingCommandMaxConcurrencyStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;

            RollingCommandMaxConcurrencyStream.Reset();
            HystrixCommandStartStream.Reset();
        }

        public override void Dispose()
        {
            base.Dispose();

            stream.Unsubscribe();
        }

        [Fact]
        public void TestEmptyStreamProducesZeros()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-A");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(
                GetSubscriber(output, latch));

            // no writes
            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        public void TestStartsAndEndsInSameBucketProduceValue()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-B");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 500);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(5).Subscribe(GetSubscriber(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100);

            cmd1.Observe();
            Time.Wait(1);

            cmd2.Observe();

            Assert.True(latch.Wait(10000));
            Assert.Equal(2, stream.LatestRollingMax);
        }

        /***
         * 3 Commands,
         * Command 1 gets started in Bucket A and not completed until Bucket B
         * Commands 2 and 3 both start and end in Bucket B, and there should be a max-concurrency of 3
         */
        [Fact]
        public void TtestOneCommandCarriesOverToNextBucket()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-C");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 160);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 15);

            cmd1.Observe();
            Time.Wait(100); // bucket roll
            cmd2.Observe();
            Time.Wait(1);
            cmd3.Observe();

            Assert.True(latch.Wait(10000));
            Assert.Equal(3, stream.LatestRollingMax);
        }

        // BUCKETS
        //     A    |    B    |    C    |    D    |    E    |
        // 1:  [-------------------------------]
        // 2:          [-------------------------------]
        // 3:                      [--]
        // 4:                              [--]
        //
        // Max concurrency should be 3
        [Fact]
        public void TestMultipleCommandsCarryOverMultipleBuckets()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-D");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

            cmd1.Observe();
            Time.Wait(100);  // bucket roll
            cmd2.Observe();
            Time.Wait(100);
            cmd3.Observe();
            Time.Wait(100);
            cmd4.Observe();

            Assert.True(latch.Wait(10000));
            Assert.Equal(3, stream.LatestRollingMax);
        }

         // BUCKETS
         //      A    |    B    |    C    |    D    |    E    |
         //  1:  [-------------------------------]
         //  2:          [-------------------------------]
         //  3:                      [--]
         //  4:                              [--]
         //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
        [Fact]
        public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-E");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(GetSubscriber(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 300);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);

            cmd1.Observe();
            Time.Wait(100); // bucket roll
            cmd2.Observe();
            Time.Wait(100);
            cmd3.Observe();
            Time.Wait(100);
            cmd4.Observe();

            Assert.True(latch.Wait(10000));
            Assert.Equal(0, stream.LatestRollingMax);
        }

        [Fact]
        public void TestConcurrencyStreamProperlyFiltersOutResponseFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-F");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 40);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

            cmd1.Observe();
            Time.Wait(5);
            cmd2.Observe();
            cmd3.Observe();
            cmd4.Observe();

            Assert.True(latch.Wait(10000));
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        public void TestConcurrencyStreamProperlyFiltersOutShortCircuits()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-G");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            // after 3 failures, next command should short-circuit.
            // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
            // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
            Command failure1 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            Command failure2 = Command.From(GroupKey, key, HystrixEventType.FAILURE);
            Command failure3 = Command.From(GroupKey, key, HystrixEventType.FAILURE);

            List<Command> shortCircuited = new List<Command>();

            for (int i = 0; i < 20; i++)
            {
                shortCircuited.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100));
            }

            failure1.Execute();
            failure2.Execute();
            failure3.Execute();

            Time.Wait(150);

            foreach (Command cmd in shortCircuited)
            {
                cmd.Observe();
            }

            Assert.True(latch.Wait(10000));
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, stream.LatestRollingMax);
        }

        [Fact]
        public void TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-H");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            // 10 commands executed concurrently on different caller threads should saturate semaphore
            // once these are in-flight, execute 10 more concurrently on new caller threads.
            // since these are semaphore-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400, ExecutionIsolationStrategy.SEMAPHORE));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100, ExecutionIsolationStrategy.SEMAPHORE));
            }

            // List<long> startTimes = new List<long>();
            foreach (Command saturatingCmd in saturators)
            {
                Task t = new Task(
                () =>
                {
                    saturatingCmd.Observe();
                }, CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();
            }

            Time.Wait(30);

            foreach (Command rejectedCmd in rejected)
            {
                Task t = new Task(
                () =>
                {
                    rejectedCmd.Observe();
                }, CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();
            }

            Assert.True(latch.Wait(10000));
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(10, stream.LatestRollingMax);
        }

        [Fact]
        public void TestConcurrencyStreamProperlyFiltersOutThreadPoolRejections()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Concurrency-I");
            stream = RollingCommandMaxConcurrencyStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(GetSubscriber(output, latch));

            // 10 commands executed concurrently should saturate the Hystrix threadpool
            // once these are in-flight, execute 10 more concurrently
            // since these are threadpool-rejected, the max concurrency should be 10
            List<Command> saturators = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                saturators.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 400));
            }

            List<Command> rejected = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                rejected.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 100));
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

            Assert.True(latch.Wait(10000));
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(10, stream.LatestRollingMax);
        }
    }
}
