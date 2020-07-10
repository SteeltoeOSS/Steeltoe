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
        private readonly IHystrixThreadPoolOptions _properties;
        private readonly IHystrixTaskScheduler _taskScheduler;
        private readonly HystrixThreadPoolMetrics _metrics;
        private readonly int _queueSize;

        public HystrixThreadPoolDefault(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions propertiesDefaults)
        {
            this._properties = HystrixOptionsFactory.GetThreadPoolOptions(threadPoolKey, propertiesDefaults);
            this._properties = propertiesDefaults ?? new HystrixThreadPoolOptions(threadPoolKey);
            HystrixConcurrencyStrategy concurrencyStrategy = HystrixPlugins.ConcurrencyStrategy;
            this._queueSize = _properties.MaxQueueSize;
            this._metrics = HystrixThreadPoolMetrics.GetInstance(threadPoolKey, concurrencyStrategy.GetTaskScheduler(_properties), _properties);
            this._taskScheduler = this._metrics.TaskScheduler;

            /* strategy: HystrixMetricsPublisherThreadPool */
            HystrixMetricsPublisherFactory.CreateOrRetrievePublisherForThreadPool(threadPoolKey, this._metrics, this._properties);
        }

        public IHystrixTaskScheduler GetScheduler()
        {
            return this._taskScheduler;
        }

        public TaskScheduler GetTaskScheduler()
        {
            return this._taskScheduler as TaskScheduler;
        }

        public void MarkThreadExecution()
        {
            _metrics.MarkThreadExecution();
        }

        public void MarkThreadCompletion()
        {
            _metrics.MarkThreadCompletion();
        }

        public void MarkThreadRejection()
        {
            _metrics.MarkThreadRejection();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this._taskScheduler.Dispose();
        }

        public bool IsQueueSpaceAvailable
        {
            get
            {
                if (_queueSize <= 0)
                {
                    // we don't have a queue so we won't look for space but instead
                    // let the thread-pool reject or not
                    return true;
                }
                else
                {
                    return _taskScheduler.IsQueueSpaceAvailable;
                }
            }
        }

        public bool IsShutdown
        {
            get { return this._taskScheduler.IsShutdown; }
        }
    }
}
