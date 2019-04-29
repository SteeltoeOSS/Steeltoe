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
