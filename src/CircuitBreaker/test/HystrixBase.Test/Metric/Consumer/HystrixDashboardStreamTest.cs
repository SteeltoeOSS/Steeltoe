// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test
{
    public class HystrixDashboardStreamTest : CommandStreamTest, IDisposable
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Dashboard");
        private static readonly IHystrixCommandKey CommandKey = HystrixCommandKeyDefault.AsKey("DashboardCommand");
        private readonly ITestOutputHelper output;
        private readonly HystrixDashboardStream stream;

        public HystrixDashboardStreamTest(ITestOutputHelper output)
        {
            stream = HystrixDashboardStream.GetNonSingletonInstanceOnlyUsedInUnitTests(10);
            this.output = output;
        }

        [Fact]
        public void TestStreamHasData()
        {
            var commandShowsUp = new AtomicBoolean(false);
            var latch = new CountdownEvent(1);
            var num = 10;

            for (var i = 0; i < 2; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Observe();
            }

            stream.Observe().Take(num).Subscribe(
                dashboardData =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : Received data with : " + dashboardData.CommandMetrics.Count + " commands");
                    foreach (var metrics in dashboardData.CommandMetrics)
                    {
                        if (metrics.CommandKey.Equals(CommandKey))
                        {
                            commandShowsUp.Value = true;
                        }
                    }
                },
                e =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
                    latch.SignalEx();
                });

            Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            Assert.True(commandShowsUp.Value);
        }

        [Fact]
        public void TestTwoSubscribersOneUnsubscribes()
        {
            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            var payloads1 = new AtomicInteger(0);
            var payloads2 = new AtomicInteger(0);

            var s1 = stream
                    .Observe()
                    .Take(100)
                    .OnDispose(() =>
                    {
                        latch1.SignalEx();
                    })
                    .Subscribe(
                        dashboardData =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + dashboardData);
                            payloads1.IncrementAndGet();
                        },
                        e =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnError : " + e);
                            latch1.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnCompleted");
                            latch1.SignalEx();
                        });

            var s2 = stream
                .Observe()
                .Take(100)
                .OnDispose(() =>
                {
                    latch2.SignalEx();
                })
                .Subscribe(
                    dashboardData =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + dashboardData);
                        payloads2.IncrementAndGet();
                    },
                    e =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnError : " + e);
                        latch2.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnCompleted");
                        latch2.SignalEx();
                    });

            // execute 1 command, then unsubscribe from first stream. then execute the rest
            for (var i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
                if (i == 1)
                {
                    s1.Dispose();
                }
            }

            Assert.True(latch1.Wait(10000));
            Assert.True(latch2.Wait(10000));
            output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
            Assert.True(payloads1.Value > 0); // "s1 got data"
            Assert.True(payloads2.Value > 0); // "s2 got data"
            Assert.True(payloads2.Value > payloads1.Value); // "s1 got less data than s2",
        }

        [Fact]
        public void TestTwoSubscribersBothUnsubscribe()
        {
            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            var payloads1 = new AtomicInteger(0);
            var payloads2 = new AtomicInteger(0);

            var s1 = stream
                    .Observe()
                    .Take(10)
                    .OnDispose(() =>
                    {
                        latch1.SignalEx();
                    })
                    .Subscribe(
                        dashboardData =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + dashboardData);
                            payloads1.IncrementAndGet();
                        },
                        e =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnError : " + e);
                            latch1.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnCompleted");
                            latch1.SignalEx();
                        });

            var s2 = stream
                .Observe()
                .Take(10)
                .OnDispose(() =>
                {
                    latch2.SignalEx();
                })
                .Subscribe(
                    dashboardData =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + dashboardData);
                        payloads2.IncrementAndGet();
                    },
                    e =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnError : " + e);
                        latch2.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnCompleted");
                        latch2.SignalEx();
                    });

            // execute half the commands, then unsubscribe from both streams, then execute the rest
            for (var i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
                if (i == 25)
                {
                    s1.Dispose();
                    s2.Dispose();
                }
            }

            Assert.False(stream.IsSourceCurrentlySubscribed);  // both subscriptions have been cancelled - source should be too

            Assert.True(latch1.Wait(10000));
            Assert.True(latch2.Wait(10000));
            output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
            Assert.True(payloads1.Value > 0); // "s1 got data",
            Assert.True(payloads2.Value > 0); // "s2 got data",
        }

        [Fact]
        public void TestTwoSubscribersOneSlowOneFast()
        {
            var latch = new CountdownEvent(1);
            var foundError = new AtomicBoolean(false);

            var fast = stream
                    .Observe()
                    .ObserveOn(NewThreadScheduler.Default);

            var slow = stream
                    .Observe()
                    .ObserveOn(NewThreadScheduler.Default)
                    .Map(n =>
                    {
                        try
                        {
                            Time.Wait(100);
                            return n;
                        }
                        catch (Exception)
                        {
                            return n;
                        }
                    });

            var checkZippedEqual = fast.Zip(slow, (payload, payload2) => payload == payload2);

            var s1 = checkZippedEqual
                    .Take(10000)
                    .Subscribe(
                        b =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + b);
                        },
                        e =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
                            output.WriteLine(e.ToString());
                            foundError.Value = true;
                            latch.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                            latch.SignalEx();
                        });

            for (var i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
            }

            latch.Wait(10000);
            Assert.False(foundError.Value);
            s1.Dispose();
        }
    }
}
