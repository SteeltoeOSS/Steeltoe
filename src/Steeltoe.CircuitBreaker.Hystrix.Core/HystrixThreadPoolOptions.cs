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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixThreadPoolOptions : HystrixBaseOptions, IHystrixThreadPoolOptions
    {
        protected const string HYSTRIX_THREADPOOL_PREFIX = "hystrix:threadpool";

        internal const int Default_CoreSize = 10; // core size of thread pool
        internal const int Default_MaximumSize = 10; // maximum size of thread pool
        internal const int Default_KeepAliveTimeMinutes = 1; // minutes to keep a thread alive
        internal const int Default_MaxQueueSize = -1; // size of queue (this can't be dynamically changed so we use 'queueSizeRejectionThreshold' to artificially limit and reject)
                                                       // -1 turns it off and makes us use SynchronousQueue
        internal const bool Default_AllowMaximumSizeToDivergeFromCoreSize = false; //should the maximumSize config value get read and used in configuring the threadPool
                                                                                           //turning this on should be a conscious decision by the user, so we default it to false

        internal const int Default_QueueSizeRejectionThreshold = 5; // number of items in queue
        internal const int Default_ThreadPoolRollingNumberStatisticalWindow = 10000; // milliseconds for rolling number
        internal const int Default_ThreadPoolRollingNumberStatisticalWindowBuckets = 10; // number of buckets in rolling number (10 1-second buckets)

        protected IHystrixThreadPoolOptions defaults;

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

        public IHystrixThreadPoolKey ThreadPoolKey { get; internal set;}

        /// <summary>
        /// Core thread-pool size that gets passed to <seealso cref="ThreadPoolExecutor#setCorePoolSize(int)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int CoreSize { get; set; }

        /// <summary>
        /// Maximum thread-pool size that gets passed to <seealso cref="ThreadPoolExecutor#setMaximumPoolSize(int)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int MaximumSize { get; set; }

        /// <summary>
        /// Keep-alive time in minutes that gets passed to <seealso cref="ThreadPoolExecutor#setKeepAliveTime(long, TimeUnit)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int KeepAliveTimeMinutes { get; set; }


        /// <summary>
        /// Max queue size that gets passed to <seealso cref="BlockingQueue"/> in <seealso cref="HystrixConcurrencyStrategy#getBlockingQueue(int)"/>
        /// 
        /// This should only affect the instantiation of a threadpool - it is not eliglible to change a queue size on the fly.
        /// For that, use <seealso cref="#queueSizeRejectionThreshold()"/>.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int MaxQueueSize { get; set; }

        /// <summary>
        /// Queue size rejection threshold is an artificial "max" size at which rejections will occur even if <seealso cref="#maxQueueSize"/> has not been reached. This is done because the <seealso cref="#maxQueueSize"/> of a
        /// <seealso cref="BlockingQueue"/> can not be dynamically changed and we want to support dynamically changing the queue size that affects rejections.
        /// <para>
        /// This is used by <seealso cref="HystrixCommand"/> when queuing a thread for execution.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int QueueSizeRejectionThreshold { get; set; }


        public virtual bool AllowMaximumSizeToDivergeFromCoreSize { get; set; } 


        /// <summary>
        /// Duration of statistical rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingNumber"/> inside each <seealso cref="HystrixThreadPoolMetrics"/> instance.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

        /// <summary>
        /// Number of buckets the rolling statistical window is broken into. This is passed into <seealso cref="HystrixRollingNumber"/> inside each <seealso cref="HystrixThreadPoolMetrics"/> instance.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        public virtual int MetricsRollingStatisticalWindowBuckets { get; set; }

    }
}
