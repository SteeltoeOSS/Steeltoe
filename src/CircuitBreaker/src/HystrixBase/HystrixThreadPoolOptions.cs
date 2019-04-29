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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixThreadPoolOptions : HystrixBaseOptions, IHystrixThreadPoolOptions
    {
        internal const int Default_CoreSize = 10; // core size of thread pool
        internal const int Default_MaximumSize = 10; // maximum size of thread pool
        internal const int Default_KeepAliveTimeMinutes = 1; // minutes to keep a thread alive
        internal const int Default_MaxQueueSize = -1; // size of queue (this can't be dynamically changed so we use 'queueSizeRejectionThreshold' to artificially limit and reject)
                                                      // -1 turns it off and makes us use SynchronousQueue
        internal const bool Default_AllowMaximumSizeToDivergeFromCoreSize = false; // should the maximumSize config value get read and used in configuring the threadPool
                                                                                   // turning this on should be a conscious decision by the user, so we default it to false
        internal const int Default_QueueSizeRejectionThreshold = 5; // number of items in queue
        internal const int Default_ThreadPoolRollingNumberStatisticalWindow = 10000; // milliseconds for rolling number
        internal const int Default_ThreadPoolRollingNumberStatisticalWindowBuckets = 10; // number of buckets in rolling number (10 1-second buckets)

        protected const string HYSTRIX_THREADPOOL_PREFIX = "hystrix:threadpool";

        protected IHystrixThreadPoolOptions defaults;

        public HystrixThreadPoolOptions(IHystrixThreadPoolKey key, IHystrixThreadPoolOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : this(defaults, dynamic)
        {
            ThreadPoolKey = key;
            AllowMaximumSizeToDivergeFromCoreSize = GetBoolean(HYSTRIX_THREADPOOL_PREFIX, key.Name, "allowMaximumSizeToDivergeFromCoreSize", Default_AllowMaximumSizeToDivergeFromCoreSize, defaults?.AllowMaximumSizeToDivergeFromCoreSize);
            CoreSize = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "coreSize", Default_CoreSize, defaults?.CoreSize);
            MaximumSize = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "maximumSize", Default_MaximumSize, defaults?.MaximumSize);
            KeepAliveTimeMinutes = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "keepAliveTimeMinutes", Default_KeepAliveTimeMinutes, defaults?.KeepAliveTimeMinutes);
            MaxQueueSize = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "maxQueueSize", Default_MaxQueueSize, defaults?.MaxQueueSize);
            QueueSizeRejectionThreshold = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "queueSizeRejectionThreshold", Default_QueueSizeRejectionThreshold, defaults?.QueueSizeRejectionThreshold);
            MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "metrics.rollingStats.timeInMilliseconds", Default_ThreadPoolRollingNumberStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds);
            MetricsRollingStatisticalWindowBuckets = GetInteger(HYSTRIX_THREADPOOL_PREFIX, key.Name, "metrics.rollingPercentile.numBuckets", Default_ThreadPoolRollingNumberStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);
        }

        internal HystrixThreadPoolOptions(IHystrixThreadPoolOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : base(dynamic)
        {
            this.defaults = defaults;
            ThreadPoolKey = null;

            AllowMaximumSizeToDivergeFromCoreSize = Default_AllowMaximumSizeToDivergeFromCoreSize;
            CoreSize = Default_CoreSize;
            MaximumSize = Default_MaximumSize;
            KeepAliveTimeMinutes = Default_KeepAliveTimeMinutes;
            MaxQueueSize = Default_MaxQueueSize;
            QueueSizeRejectionThreshold = Default_QueueSizeRejectionThreshold;
            MetricsRollingStatisticalWindowInMilliseconds = Default_ThreadPoolRollingNumberStatisticalWindow;
            MetricsRollingStatisticalWindowBuckets = Default_ThreadPoolRollingNumberStatisticalWindowBuckets;
        }

        public IHystrixThreadPoolKey ThreadPoolKey { get; internal set; }

        public virtual int CoreSize { get; set; }

        public virtual int MaximumSize { get; set; }

        public virtual int KeepAliveTimeMinutes { get; set; }

        public virtual int MaxQueueSize { get; set; }

        public virtual int QueueSizeRejectionThreshold { get; set; }

        public virtual bool AllowMaximumSizeToDivergeFromCoreSize { get; set; }

        public virtual int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

        public virtual int MetricsRollingStatisticalWindowBuckets { get; set; }
    }
}
