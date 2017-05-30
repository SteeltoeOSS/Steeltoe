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
//

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System.Collections.Concurrent;


namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics
{
    public class HystrixMetricsPublisherFactory
    {

           
        private static HystrixMetricsPublisherFactory SINGLETON = new HystrixMetricsPublisherFactory();

        public static IHystrixMetricsPublisherThreadPool CreateOrRetrievePublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
        {
            return SINGLETON.GetPublisherForThreadPool(threadPoolKey, metrics, properties);
        }

        public static IHystrixMetricsPublisherCommand CreateOrRetrievePublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
        {
            return SINGLETON.GetPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);
        }

        public static IHystrixMetricsPublisherCollapser CreateOrRetrievePublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics, IHystrixCollapserOptions properties)
        {
            return SINGLETON.GetPublisherForCollapser(collapserKey, metrics, properties);
        }

        public static void Reset()
        {
            SINGLETON = new HystrixMetricsPublisherFactory();
            SINGLETON.commandPublishers.Clear();
            SINGLETON.threadPoolPublishers.Clear();
            SINGLETON.collapserPublishers.Clear();
        }

        internal HystrixMetricsPublisherFactory() { }

        internal readonly ConcurrentDictionary<string, IHystrixMetricsPublisherCommand> commandPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherCommand>();

        internal IHystrixMetricsPublisherCommand GetPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner, HystrixCommandMetrics metrics, IHystrixCircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
        {

            return commandPublishers.GetOrAddEx(commandKey.Name, (k) =>
            {
                IHystrixMetricsPublisherCommand newPublisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);
                newPublisher.Initialize();
                return newPublisher;
            });


        }

        internal readonly ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool> threadPoolPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool>();

        internal IHystrixMetricsPublisherThreadPool GetPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
        {
            return threadPoolPublishers.GetOrAddEx(threadPoolKey.Name, (k) =>
            {
                IHystrixMetricsPublisherThreadPool publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForThreadPool(threadPoolKey, metrics, properties);
                publisher.Initialize();
                return publisher;
            });
        }


        internal readonly ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser> collapserPublishers = new ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser>();


        internal IHystrixMetricsPublisherCollapser GetPublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics, IHystrixCollapserOptions properties)
        {
            return collapserPublishers.GetOrAddEx(collapserKey.Name, (k) =>
            {
                IHystrixMetricsPublisherCollapser publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCollapser(collapserKey, metrics, properties);
                publisher.Initialize();
                return publisher;
            });
        }

    }

}
