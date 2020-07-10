// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixThreadPoolConfiguration
    {
        private readonly int _maximumSize;

        public HystrixThreadPoolConfiguration(
            IHystrixThreadPoolKey threadPoolKey,
            int coreSize,
            int maximumSize,
            int maxQueueSize,
            int queueRejectionThreshold,
            int keepAliveTimeInMinutes,
            bool allowMaximumSizeToDivergeFromCoreSize,
            int rollingCounterNumberOfBuckets,
            int rollingCounterBucketSizeInMilliseconds)
        {
            ThreadPoolKey = threadPoolKey;
            CoreSize = coreSize;
            this._maximumSize = maximumSize;
            MaxQueueSize = maxQueueSize;
            QueueRejectionThreshold = queueRejectionThreshold;
            KeepAliveTimeInMinutes = keepAliveTimeInMinutes;
            AllowMaximumSizeToDivergeFromCoreSize = allowMaximumSizeToDivergeFromCoreSize;
            RollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
            RollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
        }

        public static HystrixThreadPoolConfiguration Sample(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions threadPoolProperties)
        {
            return new HystrixThreadPoolConfiguration(
                    threadPoolKey,
                    threadPoolProperties.CoreSize,
                    threadPoolProperties.MaximumSize,
                    threadPoolProperties.MaxQueueSize,
                    threadPoolProperties.QueueSizeRejectionThreshold,
                    threadPoolProperties.KeepAliveTimeMinutes,
                    threadPoolProperties.AllowMaximumSizeToDivergeFromCoreSize,
                    threadPoolProperties.MetricsRollingStatisticalWindowBuckets,
                    threadPoolProperties.MetricsRollingStatisticalWindowInMilliseconds);
        }

        public IHystrixThreadPoolKey ThreadPoolKey { get; }

        public int CoreSize { get; }

        public int MaximumSize
        {
            get
            {
                if (AllowMaximumSizeToDivergeFromCoreSize)
                {
                    return _maximumSize;
                }
                else
                {
                    return CoreSize;
                }
            }
        }

        public int MaxQueueSize { get; }

        public int QueueRejectionThreshold { get; }

        public int KeepAliveTimeInMinutes { get; }

        public bool AllowMaximumSizeToDivergeFromCoreSize { get; }

        public int RollingCounterNumberOfBuckets { get; }

        public int RollingCounterBucketSizeInMilliseconds { get; }
    }
}
