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

using Steeltoe.CircuitBreaker.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics
{
    public class HystrixMetricsPublisherFactory
    {
        private static HystrixMetricsPublisherFactory singleton = new HystrixMetricsPublisherFactory();

        public static IHystrixMetricsPublisherThreadPool CreateOrRetrievePublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
        {
            return singleton.GetPublisherForThreadPool(threadPoolKey, metrics, properties);
        }

        public static IHystrixMetricsPublisherCommand CreateOrRetrievePublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
        {
            return singleton.GetPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);
        }

        public static IHystrixMetricsPublisherCollapser CreateOrRetrievePublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics, IHystrixCollapserOptions properties)
        {
            return singleton.GetPublisherForCollapser(collapserKey, metrics, properties);
        }

        public static void Reset()
        {
            singleton = new HystrixMetricsPublisherFactory();
            singleton.CommandPublishers.Clear();
            singleton.ThreadPoolPublishers.Clear();
            singleton.CollapserPublishers.Clear();
        }

        internal HystrixMetricsPublisherFactory()
        {
        }

        private readonly ConcurrentDictionary<string, IHystrixMetricsPublisherCommand> commandPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherCommand>();

        internal IHystrixMetricsPublisherCommand GetPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
        {
            return CommandPublishers.GetOrAddEx(commandKey.Name, (k) =>
            {
                IHystrixMetricsPublisherCommand newPublisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);
                newPublisher.Initialize();
                return newPublisher;
            });
        }

        private readonly ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool> threadPoolPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool>();

        internal IHystrixMetricsPublisherThreadPool GetPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
        {
            return ThreadPoolPublishers.GetOrAddEx(threadPoolKey.Name, (k) =>
            {
                IHystrixMetricsPublisherThreadPool publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForThreadPool(threadPoolKey, metrics, properties);
                publisher.Initialize();
                return publisher;
            });
        }

        private readonly ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser> collapserPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser>();

        internal ConcurrentDictionary<string, IHystrixMetricsPublisherCommand> CommandPublishers => commandPublishers;

        internal ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool> ThreadPoolPublishers => threadPoolPublishers;

        internal ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser> CollapserPublishers => collapserPublishers;

        internal IHystrixMetricsPublisherCollapser GetPublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics, IHystrixCollapserOptions properties)
        {
            return CollapserPublishers.GetOrAddEx(collapserKey.Name, (k) =>
            {
                IHystrixMetricsPublisherCollapser publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCollapser(collapserKey, metrics, properties);
                publisher.Initialize();
                return publisher;
            });
        }
    }
}
