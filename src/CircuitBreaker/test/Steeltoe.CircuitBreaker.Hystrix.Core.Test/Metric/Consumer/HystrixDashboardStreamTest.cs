//
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
        ITestOutputHelper output;
        HystrixDashboardStream stream;
        private readonly static IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("Dashboard");
        private readonly static IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey("DashboardCommand");

        public HystrixDashboardStreamTest(ITestOutputHelper output) : base()
        {
            this.stream = HystrixDashboardStream.GetNonSingletonInstanceOnlyUsedInUnitTests(10);
            this.output = output;

        }


        [Fact]
        public void TestStreamHasData()
        {
            AtomicBoolean commandShowsUp = new AtomicBoolean(false);
            CountdownEvent latch = new CountdownEvent(1);
            int NUM = 10;

            for (int i = 0; i < 2; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, commandKey, HystrixEventType.SUCCESS, 50);
                cmd.Observe();
            }

            stream.Observe().Take(NUM).Subscribe(
                (dashboardData) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : Received data with : " + dashboardData.commandMetrics.Count + " commands");
                    foreach (HystrixCommandMetrics metrics in dashboardData.commandMetrics)
                    {
                        if (metrics.CommandKey.Equals(commandKey))
                        {
                            commandShowsUp.Value = true;
                        }
                    }
                },
                (e) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
                    latch.SignalEx();
                });



            Assert.True(latch.Wait(10000));
            Assert.True(commandShowsUp.Value);
        }
        [Fact]
        public void TestTwoSubscribersOneUnsubscribes()
        {
            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);
            AtomicInteger payloads1 = new AtomicInteger(0);
            AtomicInteger payloads2 = new AtomicInteger(0);

            IDisposable s1 = stream
                    .Observe()
                    .Take(100)
                    .OnDispose(() =>
                    {

                        latch1.SignalEx();

                    })
                    .Subscribe(
                        (dashboardData) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + dashboardData);
                            payloads1.IncrementAndGet();
                        },
                        (e) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnError : " + e);
                            latch1.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnCompleted");
                            latch1.SignalEx();
                        });


            IDisposable s2 = stream
                .Observe()
                .Take(100)
                .OnDispose(() =>
                {
                    latch2.SignalEx();
                })
                .Subscribe(
                    (dashboardData) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + dashboardData);
                        payloads2.IncrementAndGet();
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnError : " + e);
                        latch2.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnCompleted");
                        latch2.SignalEx();
                    });

            //execute 1 command, then unsubscribe from first stream. then execute the rest
            for (int i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, commandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
                if (i == 1)
                {
                    s1.Dispose();
                }
            }

            Assert.True(latch1.Wait(10000));
            Assert.True(latch2.Wait(10000));
            output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
            Assert.True(payloads1.Value > 0); //"s1 got data"
            Assert.True(payloads2.Value > 0); //"s2 got data"
            Assert.True(payloads2.Value > payloads1.Value); //"s1 got less data than s2",
        }
        [Fact]
        public void TestTwoSubscribersBothUnsubscribe()
        {
            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);
            AtomicInteger payloads1 = new AtomicInteger(0);
            AtomicInteger payloads2 = new AtomicInteger(0);

            IDisposable s1 = stream
                    .Observe()
                    .Take(10)
                    .OnDispose(() =>
                    {
                        latch1.SignalEx();
                    })
                    .Subscribe(
                        (dashboardData) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + dashboardData);
                            payloads1.IncrementAndGet();
                        },
                        (e) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnError : " + e);
                            latch1.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnCompleted");
                            latch1.SignalEx();
                        });


            IDisposable s2 = stream
                .Observe()
                .Take(10)
                .OnDispose(() =>
                {
                    latch2.SignalEx();

                })
                .Subscribe(
                    (dashboardData) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + dashboardData);
                        payloads2.IncrementAndGet();
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnError : " + e);
                        latch2.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnCompleted");
                        latch2.SignalEx();
                    });

            //execute half the commands, then unsubscribe from both streams, then execute the rest
            for (int i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, commandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
                if (i == 25)
                {
                    s1.Dispose();
                    s2.Dispose();
                }
            }
            Assert.False(stream.IsSourceCurrentlySubscribed);  //both subscriptions have been cancelled - source should be too

            Assert.True(latch1.Wait(10000));
            Assert.True(latch2.Wait(10000));
            output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
            Assert.True(payloads1.Value > 0); //"s1 got data",
            Assert.True(payloads2.Value > 0); //"s2 got data",
        }

        [Fact]
        public void TestTwoSubscribersOneSlowOneFast()
        {
            CountdownEvent latch = new CountdownEvent(1);
            AtomicBoolean foundError = new AtomicBoolean(false);

            IObservable<HystrixDashboardStream.DashboardData> fast = stream
                    .Observe()
                    .ObserveOn(NewThreadScheduler.Default);

            IObservable<HystrixDashboardStream.DashboardData> slow = stream
                    .Observe()
                    .ObserveOn(NewThreadScheduler.Default)
                    .Map((n) =>
                    {

                        try
                        {

                            Time.Wait( 100);
                            return n;
                        }
                        catch (Exception )
                        {
                            return n;
                        }

                    });

            IObservable<Boolean> checkZippedEqual = Observable.Zip(fast, slow, (payload, payload2) =>
                {
                    return payload == payload2;

                });

            IDisposable s1 = checkZippedEqual
                    .Take(10000)
                    .Subscribe(
                        (b) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + b);
                        },
                        (e) =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
                            output.WriteLine(e.ToString());
                            foundError.Value = true;
                            latch.SignalEx();
                        },
                        () =>
                        {
                            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                            latch.SignalEx();
                        });

            for (int i = 0; i < 50; i++)
            {
                HystrixCommand<int> cmd = Command.From(groupKey, commandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
            }

            latch.Wait(10000);
            Assert.False(foundError.Value);
            s1.Dispose();
        }

    }
}
