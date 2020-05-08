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
            private readonly CountdownEvent latch;

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
        private readonly ITestOutputHelper output;

        public HystrixCommandCompletionStreamTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestNoEvents()
        {
            var latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            // no writes
            Assert.False(latch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestSingleWriteSingleSubscriber()
        {
            var latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            var result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            var @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
            commandStream.Write(@event);

            Assert.True(latch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestSingleWriteMultipleSubscribers()
        {
            var latch1 = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber1 = new LatchedObserver(latch1);

            var latch2 = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber2 = new LatchedObserver(latch2);

            commandStream.Observe().Take(1).Subscribe(subscriber1);
            commandStream.Observe().Take(1).Subscribe(subscriber2);

            var result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            var @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
            commandStream.Write(@event);

            Assert.True(latch1.Wait(TimeSpan.FromMilliseconds(1000)));
            Assert.True(latch2.Wait(TimeSpan.FromMilliseconds(10)));
        }
    }
}
