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
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.CircuitBreaker.Util;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics.Test
{
    public class HystrixMetricsPublisherFactoryTest : HystrixTestBase
    {
        private ITestOutputHelper output;

        public HystrixMetricsPublisherFactoryTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestSingleInitializePerKey()
        {
            TestHystrixMetricsPublisher publisher = new TestHystrixMetricsPublisher();
            HystrixPlugins.RegisterMetricsPublisher(publisher);
            HystrixMetricsPublisherFactory factory = new HystrixMetricsPublisherFactory();
            List<Task> threads = new List<Task>();
            for (int i = 0; i < 20; i++)
            {
                threads.Add(new Task(
                    () =>
                    {
                        factory.GetPublisherForCommand(TestCommandKey.TEST_A, null, null, null, null);
                        factory.GetPublisherForCommand(TestCommandKey.TEST_B, null, null, null, null);
                        factory.GetPublisherForThreadPool(TestThreadPoolKey.TEST_A, null, null);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning));
            }

            // start them
            foreach (Task t in threads)
            {
                t.Start();
            }

            // wait for them to finish
            Task.WaitAll(threads.ToArray());

            Assert.Equal(2, factory.CommandPublishers.Count);
            Assert.Single(factory.ThreadPoolPublishers);

            // we should see 2 commands and 1 threadPool publisher created
            Assert.Equal(2, publisher.CommandCounter.Value);
            Assert.Equal(1, publisher.ThreadCounter.Value);
        }

        [Fact]
        public void TestMetricsPublisherReset()
        {
            // precondition: HystrixMetricsPublisherFactory class is not loaded. Calling HystrixPlugins.reset() here should be good enough to run this with other tests.

            // set first custom publisher
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("key");
            IHystrixMetricsPublisherCommand firstCommand = new HystrixMetricsPublisherCommandDefault(key, null, null, null, null);
            HystrixMetricsPublisher firstPublisher = new CustomPublisher(firstCommand);
            HystrixPlugins.RegisterMetricsPublisher(firstPublisher);

            // ensure that first custom publisher is used
            IHystrixMetricsPublisherCommand cmd = HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(key, null, null, null, null);
            Assert.True(firstCommand == cmd);

            // reset, then change to second custom publisher
            HystrixPlugins.Reset();
            IHystrixMetricsPublisherCommand secondCommand = new HystrixMetricsPublisherCommandDefault(key, null, null, null, null);
            HystrixMetricsPublisher secondPublisher = new CustomPublisher(secondCommand);
            HystrixPlugins.RegisterMetricsPublisher(secondPublisher);

            // ensure that second custom publisher is used
            cmd = HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(key, null, null, null, null);
            Assert.True(firstCommand != cmd);
            Assert.True(secondCommand == cmd);
        }

        private class MyHystrixMetricsPublisherCommand : IHystrixMetricsPublisherCommand
        {
            private AtomicInteger commandCounter;

            public MyHystrixMetricsPublisherCommand(AtomicInteger commandCounter)
            {
                this.commandCounter = commandCounter;
            }

            public void Initialize()
            {
                commandCounter.IncrementAndGet();
            }
        }

        private class MyHystrixMetricsPublisherThreadPool : IHystrixMetricsPublisherThreadPool
        {
            private AtomicInteger threadCounter;

            public MyHystrixMetricsPublisherThreadPool(AtomicInteger threadCounter)
            {
                this.threadCounter = threadCounter;
            }

            public void Initialize()
            {
                threadCounter.IncrementAndGet();
            }
        }

        private class TestHystrixMetricsPublisher : HystrixMetricsPublisher
        {
            public AtomicInteger CommandCounter = new AtomicInteger();
            public AtomicInteger ThreadCounter = new AtomicInteger();

            public override IHystrixMetricsPublisherCommand GetMetricsPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
            {
                return new MyHystrixMetricsPublisherCommand(CommandCounter);
            }

            public override IHystrixMetricsPublisherThreadPool GetMetricsPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
            {
                return new MyHystrixMetricsPublisherThreadPool(ThreadCounter);
            }
        }

        private class TestCommandKey : HystrixCommandKeyDefault
        {
            public static TestCommandKey TEST_A = new TestCommandKey("TEST_A");
            public static TestCommandKey TEST_B = new TestCommandKey("TEST_B");

            public TestCommandKey(string name)
                : base(name)
            {
            }
        }

        private class TestThreadPoolKey : HystrixThreadPoolKeyDefault
        {
            public static TestThreadPoolKey TEST_A = new TestThreadPoolKey("TEST_A");
            public static TestThreadPoolKey TEST_B = new TestThreadPoolKey("TEST_B");

            public TestThreadPoolKey(string name)
                : base(name)
            {
            }
        }

        private class CustomPublisher : HystrixMetricsPublisher
        {
            private IHystrixMetricsPublisherCommand commandToReturn;

            public CustomPublisher(IHystrixMetricsPublisherCommand commandToReturn)
            {
                this.commandToReturn = commandToReturn;
            }

            public override IHystrixMetricsPublisherCommand GetMetricsPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandGroupKey, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
            {
                return commandToReturn;
            }
        }
    }
}
