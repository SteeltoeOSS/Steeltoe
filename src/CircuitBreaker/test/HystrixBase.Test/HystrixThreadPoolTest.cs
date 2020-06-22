﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixThreadPoolTest : HystrixTestBase, IDisposable
    {
        private ITestOutputHelper output;

        public HystrixThreadPoolTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public void TestShutdown()
        {
            // other unit tests will probably have run before this so get the count
            int count = HystrixThreadPoolFactory.ThreadPools.Count;

            IHystrixThreadPool pool = HystrixThreadPoolFactory.GetInstance(
                HystrixThreadPoolKeyDefault.AsKey("threadPoolFactoryTest"),
                HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder());

            Assert.Equal(count + 1, HystrixThreadPoolFactory.ThreadPools.Count);
            Assert.False(pool.GetScheduler().IsShutdown);

            HystrixThreadPoolFactory.Shutdown();

            // ensure all pools were removed from the cache
            Assert.Empty(HystrixThreadPoolFactory.ThreadPools);
            Assert.True(pool.GetScheduler().IsShutdown);
        }

        private class HystrixMetricsPublisherThreadPoolContainer : IHystrixMetricsPublisherThreadPool
        {
            private readonly HystrixThreadPoolMetrics hystrixThreadPoolMetrics;

            public HystrixMetricsPublisherThreadPoolContainer(HystrixThreadPoolMetrics hystrixThreadPoolMetrics)
            {
                this.hystrixThreadPoolMetrics = hystrixThreadPoolMetrics;
            }

            public void Initialize()
            {
            }

            public HystrixThreadPoolMetrics HystrixThreadPoolMetrics
            {
                get { return hystrixThreadPoolMetrics; }
            }
        }

        private class MyHystrixMetricsPublisher : HystrixMetricsPublisher
        {
            public override IHystrixMetricsPublisherThreadPool GetMetricsPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
            {
                return new HystrixMetricsPublisherThreadPoolContainer(metrics);
            }
        }

        [Fact]
        public void EnsureThreadPoolInstanceIsTheOneRegisteredWithMetricsPublisherAndThreadPoolCache()
        {
            HystrixPlugins.RegisterMetricsPublisher(new MyHystrixMetricsPublisher());

            IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("threadPoolFactoryConcurrencyTest");
            IHystrixThreadPool poolOne = new HystrixThreadPoolDefault(
                    threadPoolKey, HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder());
            IHystrixThreadPool poolTwo = new HystrixThreadPoolDefault(
                    threadPoolKey, HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder());

            Assert.Equal(poolOne.GetScheduler(), poolTwo.GetScheduler()); // Now that we get the threadPool from the metrics object, this will always be equal
            HystrixMetricsPublisherThreadPoolContainer hystrixMetricsPublisherThreadPool =
                    (HystrixMetricsPublisherThreadPoolContainer)HystrixMetricsPublisherFactory
                            .CreateOrRetrievePublisherForThreadPool(threadPoolKey, null, null);
            IHystrixTaskScheduler threadPoolExecutor = hystrixMetricsPublisherThreadPool.HystrixThreadPoolMetrics.TaskScheduler;

            // assert that both HystrixThreadPools share the same ThreadPoolExecutor as the one in HystrixMetricsPublisherThreadPool
            Assert.True(threadPoolExecutor.Equals(poolOne.GetScheduler()) && threadPoolExecutor.Equals(poolTwo.GetScheduler()));
            Assert.False(threadPoolExecutor.IsShutdown);

            // Now the HystrixThreadPool ALWAYS has the same reference to the ThreadPoolExecutor so that it no longer matters which
            // wins to be inserted into the HystrixThreadPool.Factory.threadPools cache.
            poolOne.Dispose();
            poolTwo.Dispose();
        }
    }
}
