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


namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixThreadPoolOptions
    {
        IHystrixThreadPoolKey ThreadPoolKey { get; }

        /// <summary>
        /// Core thread-pool size that gets passed to <seealso cref="ThreadPoolExecutor#setCorePoolSize(int)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int CoreSize { get; set; }

        /// <summary>
        /// Maximum thread-pool size that gets passed to <seealso cref="ThreadPoolExecutor#setMaximumPoolSize(int)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int MaximumSize { get; set; }

        /// <summary>
        /// Keep-alive time in minutes that gets passed to <seealso cref="ThreadPoolExecutor#setKeepAliveTime(long, TimeUnit)"/>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
       int KeepAliveTimeMinutes { get; set; }


        /// <summary>
        /// Max queue size that gets passed to <seealso cref="BlockingQueue"/> in <seealso cref="HystrixConcurrencyStrategy#getBlockingQueue(int)"/>
        /// 
        /// This should only affect the instantiation of a threadpool - it is not eliglible to change a queue size on the fly.
        /// For that, use <seealso cref="#queueSizeRejectionThreshold()"/>.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int MaxQueueSize { get; set; }

        /// <summary>
        /// Queue size rejection threshold is an artificial "max" size at which rejections will occur even if <seealso cref="#maxQueueSize"/> has not been reached. This is done because the <seealso cref="#maxQueueSize"/> of a
        /// <seealso cref="BlockingQueue"/> can not be dynamically changed and we want to support dynamically changing the queue size that affects rejections.
        /// <para>
        /// This is used by <seealso cref="HystrixCommand"/> when queuing a thread for execution.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int QueueSizeRejectionThreshold { get; set; }


        bool AllowMaximumSizeToDivergeFromCoreSize { get; set; }


        /// <summary>
        /// Duration of statistical rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingNumber"/> inside each <seealso cref="HystrixThreadPoolMetrics"/> instance.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

        /// <summary>
        /// Number of buckets the rolling statistical window is broken into. This is passed into <seealso cref="HystrixRollingNumber"/> inside each <seealso cref="HystrixThreadPoolMetrics"/> instance.
        /// </summary>
        /// <returns> {@code HystrixProperty<Integer>} </returns>
        int MetricsRollingStatisticalWindowBuckets { get; set; }

    }
}
