﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test
{
    public class HystrixCommandCompletionStreamTest : CommandStreamTest
    {
        private class LatchedObserver : ObserverBase<HystrixCommandCompletion>
        {
            private CountdownEvent latch;

            public LatchedObserver(CountdownEvent latch)
            {
                this.latch = latch;
            }

            protected override void OnCompletedCore()
            {
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                Assert.False(true, error.Message);
            }

            protected override void OnNextCore(HystrixCommandCompletion value)
            {
            }
        }

        private static readonly IHystrixCommandKey CommandKey = HystrixCommandKeyDefault.AsKey("COMMAND");
        private static readonly IHystrixThreadPoolKey ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool");
        private readonly HystrixCommandCompletionStream commandStream = new HystrixCommandCompletionStream(CommandKey);
        private ITestOutputHelper output;

        public HystrixCommandCompletionStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestNoEvents()
        {
            CountdownEvent latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            // no writes
            Assert.False(latch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestSingleWriteSingleSubscriber()
        {
            CountdownEvent latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            HystrixCommandCompletion @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
            commandStream.Write(@event);

            Assert.True(latch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestSingleWriteMultipleSubscribers()
        {
            CountdownEvent latch1 = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber1 = new LatchedObserver(latch1);

            CountdownEvent latch2 = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber2 = new LatchedObserver(latch2);

            commandStream.Observe().Take(1).Subscribe(subscriber1);
            commandStream.Observe().Take(1).Subscribe(subscriber2);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            HystrixCommandCompletion @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
            commandStream.Write(@event);

            Assert.True(latch1.Wait(TimeSpan.FromMilliseconds(1000)));
            Assert.True(latch2.Wait(TimeSpan.FromMilliseconds(10)));
        }
    }
}
