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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class RollingCommandLatencyDistributionStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CommandLatency");
        private RollingCommandLatencyDistributionStream stream;
        private ITestOutputHelper output;

        public RollingCommandLatencyDistributionStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            RollingCommandLatencyDistributionStream.Reset();
            HystrixCommandCompletionStream.Reset();
        }

        public override void Dispose()
        {
            base.Dispose();
            stream.Unsubscribe();
        }

        [Fact]
        public void TestEmptyStreamProducesEmptyDistributions()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-A");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
                (distribution) =>
                {
                    output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + Thread.CurrentThread.ManagedThreadId);
                    Assert.Equal(0, distribution.GetTotalCount());
                },
                (e) =>
                {
                    Assert.True(false, e.Message);
                },
                () =>
                {
                    latch.SignalEx();
                });

            // no writes
            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestSingleBucketGetsStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-B");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() == 1)
                {
                    AssertBetween(10, 50, (int)distribution.GetMean());
                }
                else if (distribution.GetTotalCount() == 2)
                {
                    AssertBetween(300, 400, (int)distribution.GetMean());
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            cmd1.Observe();
            cmd2.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }
            catch (Exception)
            {
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.True(false, "Interrupted ex");
            }

            AssertBetween(150, 400, stream.LatestMean);
            AssertBetween(10, 50, stream.GetLatestPercentile(0.0));
            AssertBetween(300, 800, stream.GetLatestPercentile(100.0));
        }

        // The following event types should not have their latency measured:
        // THREAD_POOL_REJECTED
        // SEMAPHORE_REJECTED
        // SHORT_CIRCUITED
        // RESPONSE_FROM_CACHE
        // Newly measured (as of 1.5)
        // BAD_REQUEST
        // FAILURE
        // TIMEOUT
        [Fact]
        public void TestSingleBucketWithMultipleEventTypes()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-C");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() < 4 && distribution.GetTotalCount() > 0)
                {
                    // buckets before timeout latency registers
                    AssertBetween(10, 50, distribution.GetMean());
                }
                else if (distribution.GetTotalCount() == 4)
                {
                    AssertBetween(150, 250, distribution.GetMean()); // now timeout latency of 600ms is there
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.TIMEOUT); // latency = 600
            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 30);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.BAD_REQUEST, 40);

            cmd1.Observe();
            cmd2.Observe();
            cmd3.Observe();
            cmd4.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            AssertBetween(150, 350, stream.LatestMean); // now timeout latency of 600ms is there
            AssertBetween(10, 40, stream.GetLatestPercentile(0.0));
            AssertBetween(600, 800, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void TestShortCircuitedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-D");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 3 failures is enough to trigger short-circuit.  execute those, then wait for bucket to roll
            // next command should be a short-circuit
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 3; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.FAILURE, 0));
            }

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                AssertBetween(0, 30, (int)distribution.GetMean());
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                cmd.Observe();
            }

            Command shortCircuit = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            try
            {
                Time.Wait(200);
                shortCircuit.Observe();
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(3, stream.Latest.GetTotalCount());
            AssertBetween(0, 30, stream.LatestMean);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(shortCircuit.IsResponseShortCircuited);
        }

        [Fact]
        public void TestThreadPoolRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-E");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 10 commands with latency should occupy the entire threadpool.  execute those, then wait for bucket to roll
            // next command should be a thread-pool rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 200));
            }

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() > 0)
                {
                    AssertBetween(200, 250, (int)distribution.GetMean());
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                cmd.Observe();
            }

            Command threadPoolRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            try
            {
                Time.Wait(40);
                threadPoolRejected.Observe();
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception ex)
            {
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Null(ex);
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(200, 250, stream.LatestMean);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(threadPoolRejected.IsResponseThreadPoolRejected);
        }

        [Fact]
        public void TestSemaphoreRejectedCommandDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-F");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // 10 commands with latency should occupy all semaphores.  execute those, then wait for bucket to roll
            // next command should be a semaphore rejection
            List<Command> commands = new List<Command>();
            for (int i = 0; i < 10; i++)
            {
                commands.Add(Command.From(GroupKey, key, HystrixEventType.SUCCESS, 200, ExecutionIsolationStrategy.SEMAPHORE));
            }

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() > 0)
                {
                    AssertBetween(200, 250, (int)distribution.GetMean());
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                Task t = new Task(
                () =>
                {
                    cmd.Observe();
                }, CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                t.Start();
            }

            Command semaphoreRejected = Command.From(GroupKey, key, HystrixEventType.SUCCESS);

            try
            {
                Time.Wait(40);
                semaphoreRejected.Observe();
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(10, stream.Latest.GetTotalCount());
            AssertBetween(200, 250, stream.LatestMean);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(semaphoreRejected.IsResponseSemaphoreRejected);
        }

        [Fact]
        public void TestResponseFromCacheDoesNotGetLatencyTracked()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-G");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            // should get 1 SUCCESS and 1 RESPONSE_FROM_CACHE
            List<Command> commands = Command.GetCommandsWithResponseFromCache(GroupKey, key);

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                Assert.True(distribution.GetTotalCount() <= 1);
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            foreach (Command cmd in commands)
            {
                cmd.Observe();
            }

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.False(true, "Interrupted ex");
            }

            Assert.Equal(1, stream.Latest.GetTotalCount());
            AssertBetween(0, 30, stream.LatestMean);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }

        [Fact]
        public void TestMultipleBucketsBothGetStored()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-H");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(10).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() == 2)
                {
                    AssertBetween(55, 90, (int)distribution.GetMean());
                }
                if (distribution.GetTotalCount() == 5)
                {
                    AssertBetween(60, 90, (int)distribution.GetMean());
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            cmd1.Observe();
            cmd2.Observe();

            try
            {
                Time.Wait(500);
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            cmd3.Observe();
            cmd4.Observe();
            cmd5.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            AssertBetween(55, 90, stream.LatestMean);
            AssertBetween(10, 50, stream.GetLatestPercentile(0.0));
            AssertBetween(100, 150, stream.GetLatestPercentile(100.0));
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleBucketsBothGetStoredAndThenAgeOut()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-I");
            stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
            stream.StartCachingStreamValuesIfUnstarted();

            CountdownEvent latch = new CountdownEvent(1);
            stream.Observe().Take(30).Subscribe(
            (distribution) =>
            {
                output.WriteLine("OnNext @ " + (DateTime.Now.Ticks / 10000) + " : " + distribution.GetMean() + "/" + distribution.GetTotalCount() + " " + Thread.CurrentThread.ManagedThreadId);
                if (distribution.GetTotalCount() == 2)
                {
                    AssertBetween(55, 90, distribution.GetMean());
                }
                if (distribution.GetTotalCount() == 5)
                {
                    AssertBetween(60, 90, distribution.GetMean());
                }
            },
            (e) =>
            {
                Assert.True(false, e.Message);
            },
            () =>
            {
                latch.SignalEx();
            });

            Command cmd1 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 10);
            Command cmd2 = Command.From(GroupKey, key, HystrixEventType.FAILURE, 100);

            cmd1.Observe();
            cmd2.Observe();

            try
            {
                Time.Wait(500);
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Command cmd3 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd4 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 60);
            Command cmd5 = Command.From(GroupKey, key, HystrixEventType.SUCCESS, 70);

            cmd3.Observe();
            cmd4.Observe();
            cmd5.Observe();

            try
            {
                Assert.True(latch.Wait(10000));
            }
            catch (Exception)
            {
                Assert.True(false, "Interrupted ex");
            }

            Assert.Equal(0, stream.Latest.GetTotalCount());
        }

        private void AssertBetween(int expectedLow, int expectedHigh, int value)
        {
            output.WriteLine("Low:" + expectedLow + " High:" + expectedHigh + " Value: " + value);
            Assert.True(expectedLow <= value);
            Assert.True(expectedHigh >= value);
        }
    }
}
