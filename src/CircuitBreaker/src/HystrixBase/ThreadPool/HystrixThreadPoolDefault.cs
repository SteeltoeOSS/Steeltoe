// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
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
