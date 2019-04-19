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
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Config.Test
{
    public class HystrixConfigurationStreamTest : CommandStreamTest
    {
        private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Config");
        private static readonly IHystrixCommandKey CommandKey = HystrixCommandKeyDefault.AsKey("Command");

        private ITestOutputHelper output;
        private HystrixConfigurationStream stream;

        public HystrixConfigurationStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            stream = HystrixConfigurationStream.GetNonSingletonInstanceOnlyUsedInUnitTests(10);
        }

        [Fact]
        public void TestStreamHasData()
        {
            AtomicBoolean commandShowsUp = new AtomicBoolean(false);
            AtomicBoolean threadPoolShowsUp = new AtomicBoolean(false);
            CountdownEvent latch = new CountdownEvent(1);
            int num = 10;

            for (int i = 0; i < 2; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Observe();
            }

            stream.Observe().Take(num).Subscribe(
                (configuration) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " : Received data with : " + configuration.CommandConfig.Count + " commands");
                    if (configuration.CommandConfig.ContainsKey(CommandKey))
                    {
                        commandShowsUp.Value = true;
                    }
                    if (configuration.ThreadPoolConfig.Count != 0)
                    {
                        threadPoolShowsUp.Value = true;
                    }
                },
                (e) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
                    latch.SignalEx();
                },
                () =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
                    latch.SignalEx();
                });

            Assert.True(latch.Wait(10000));
            Assert.True(commandShowsUp.Value);
            Assert.True(threadPoolShowsUp.Value);
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
                    (configuration) =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + configuration.CommandConfig.Count + " commands");
                        payloads1.IncrementAndGet();
                    },
                    (e) =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1  OnError : " + e);
                        latch1.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnCompleted");
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
                (configuration) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + configuration.CommandConfig.Count + " commands");
                    payloads2.IncrementAndGet();
                },
                (e) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2  OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnCompleted");
                    latch2.SignalEx();
                });

            // execute 1 command, then unsubscribe from first stream. then execute the rest
            for (int i = 0; i < 50; i++)
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
            Assert.True(payloads1.Value > 0); // "s1 got data",
            Assert.True(payloads2.Value > 0); // "s2 got data",
            Assert.True(payloads2.Value > payloads1.Value); // "s1 got less data than s2",
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
                .Take(100)
                .OnDispose(() =>
                {
                    latch1.SignalEx();
                })
                .Subscribe(
                (configuration) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + configuration.CommandConfig.Count + " commands");
                    payloads1.IncrementAndGet();
                },
                (e) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1  OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnCompleted");
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
                (configuration) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + configuration.CommandConfig.Count + " commands");
                    payloads2.IncrementAndGet();
                },
                (e) =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2  OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnCompleted");
                    latch2.SignalEx();
                });

            // execute 2 commands, then unsubscribe from both streams, then execute the rest
            for (int i = 0; i < 10; i++)
            {
                HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.SUCCESS, 50);
                cmd.Execute();
                if (i == 2)
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
            CountdownEvent latch = new CountdownEvent(1);
            AtomicBoolean foundError = new AtomicBoolean(false);

            IObservable<HystrixConfiguration> fast = stream
                    .Observe()
                    .ObserveOn(NewThreadScheduler.Default);

            IObservable<HystrixConfiguration> slow = stream
                     .Observe()
                     .ObserveOn(NewThreadScheduler.Default)
                     .Map(
                (HystrixConfiguration config) =>
             {
                 try
                 {
                     Time.Wait(100);
                     return config;
                 }
                 catch (Exception)
                 {
                     return config;
                 }
             });

            IObservable<bool> checkZippedEqual = Observable.Zip(fast, slow, (HystrixConfiguration payload, HystrixConfiguration payload2) =>
            {
                return payload == payload2;
            });

            IDisposable s1 = checkZippedEqual
                    .Take(10000)
                    .Subscribe(
                    (b) =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " :  OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
                        foundError.Value = true;
                        latch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine((DateTime.Now.Ticks / 10000) + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
                        latch.SignalEx();
                    });

            for (int i = 0; i < 50; i++)
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
