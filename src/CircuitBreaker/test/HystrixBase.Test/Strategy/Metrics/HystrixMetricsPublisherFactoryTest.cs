// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics.Test
{
    public class HystrixMetricsPublisherFactoryTest : HystrixTestBase
    {
        private readonly ITestOutputHelper output;

        public HystrixMetricsPublisherFactoryTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestSingleInitializePerKey()
        {
            var publisher = new TestHystrixMetricsPublisher();
            HystrixPlugins.RegisterMetricsPublisher(publisher);
            var factory = new HystrixMetricsPublisherFactory();
            var threads = new List<Task>();
            for (var i = 0; i < 20; i++)
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
            foreach (var t in threads)
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
            var key = HystrixCommandKeyDefault.AsKey("key");
            IHystrixMetricsPublisherCommand firstCommand = new HystrixMetricsPublisherCommandDefault(key, null, null, null, null);
            HystrixMetricsPublisher firstPublisher = new CustomPublisher(firstCommand);
            HystrixPlugins.RegisterMetricsPublisher(firstPublisher);

            // ensure that first custom publisher is used
            var cmd = HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForCommand(key, null, null, null, null);
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
            private readonly AtomicInteger commandCounter;

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
            private readonly AtomicInteger threadCounter;

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

            public override IHystrixMetricsPublisherCommand GetMetricsPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
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
            private readonly IHystrixMetricsPublisherCommand commandToReturn;

            public CustomPublisher(IHystrixMetricsPublisherCommand commandToReturn)
            {
                this.commandToReturn = commandToReturn;
            }

            public override IHystrixMetricsPublisherCommand GetMetricsPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandGroupKey, HystrixCommandMetrics metrics, ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
            {
                return commandToReturn;
            }
        }
    }
}
