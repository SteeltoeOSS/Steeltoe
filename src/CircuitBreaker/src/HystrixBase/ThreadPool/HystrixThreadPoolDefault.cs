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
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.ThreadPool
{
    public class HystrixThreadPoolDefault : IHystrixThreadPool
    {
        private readonly IHystrixThreadPoolOptions properties;
        private readonly IHystrixTaskScheduler taskScheduler;
        private readonly HystrixThreadPoolMetrics metrics;
        private readonly int queueSize;

        public HystrixThreadPoolDefault(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions propertiesDefaults)
        {
            this.properties = HystrixOptionsFactory.GetThreadPoolOptions(threadPoolKey, propertiesDefaults);
            this.properties = propertiesDefaults ?? new HystrixThreadPoolOptions(threadPoolKey);
            HystrixConcurrencyStrategy concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
            this.queueSize = properties.MaxQueueSize;
            this.metrics = HystrixThreadPoolMetrics.GetInstance(threadPoolKey, concurrencyStrategy.GetTaskScheduler(properties), properties);
            this.taskScheduler = this.metrics.TaskScheduler;

            /* strategy: HystrixMetricsPublisherThreadPool */
            HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForThreadPool(threadPoolKey, this.metrics, this.properties);
        }

        public IHystrixTaskScheduler GetScheduler()
        {
            return this.taskScheduler;
        }

        public TaskScheduler GetTaskScheduler()
        {
            return this.taskScheduler as TaskScheduler;
        }

        public void MarkThreadExecution()
        {
            metrics.MarkThreadExecution();
        }

        public void MarkThreadCompletion()
        {
            metrics.MarkThreadCompletion();
        }

        public void MarkThreadRejection()
        {
            metrics.MarkThreadRejection();
        }

        public void Dispose()
        {
            this.taskScheduler.Dispose();
        }

        public bool IsQueueSpaceAvailable
        {
            get
            {
                if (queueSize <= 0)
                {
                    // we don't have a queue so we won't look for space but instead
                    // let the thread-pool reject or not
                    return true;
                }
                else
                {
                    return taskScheduler.IsQueueSpaceAvailable;
                }
            }
        }

        public bool IsShutdown
        {
            get { return this.taskScheduler.IsShutdown; }
        }
    }
}
