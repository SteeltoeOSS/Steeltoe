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

using Xunit;
using System.Threading;
using System;
using System.Reactive.Linq;
using System.Reactive;
using Xunit.Abstractions;
using Steeltoe.CircuitBreaker.Hystrix.Test;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test
{
    class LatchedObserver : ObserverBase<HystrixCommandCompletion>
    {
        CountdownEvent latch;
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

    public class HystrixCommandCompletionStreamTest : CommandStreamTest
    {

        ITestOutputHelper output;
        static readonly IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey("COMMAND");
        static readonly IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool");
        readonly HystrixCommandCompletionStream commandStream = new HystrixCommandCompletionStream(commandKey);
        public HystrixCommandCompletionStreamTest(ITestOutputHelper output) : base()
        {
            this.output = output;
        }


        [Fact]
        public void TestNoEvents()
        {
            CountdownEvent latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            //no writes

            Assert.False(latch.Wait(TimeSpan.FromMilliseconds(1000)));
        }

        [Fact]
        public void TestSingleWriteSingleSubscriber()
        {
            CountdownEvent latch = new CountdownEvent(1);
            IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

            commandStream.Observe().Take(1).Subscribe(subscriber);

            ExecutionResult result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
            HystrixCommandCompletion @event = HystrixCommandCompletion.From(result, commandKey, threadPoolKey);
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
            HystrixCommandCompletion @event = HystrixCommandCompletion.From(result, commandKey, threadPoolKey);
            commandStream.Write(@event);

            Assert.True(latch1.Wait(TimeSpan.FromMilliseconds(1000)));
            Assert.True(latch2.Wait(TimeSpan.FromMilliseconds(10)));
        }
    }
}
