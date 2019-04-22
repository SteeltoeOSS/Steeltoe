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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixThreadPoolConfiguration
    {
        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly int coreSize;
        private readonly int maximumSize;
        private readonly int maxQueueSize;
        private readonly int queueRejectionThreshold;
        private readonly int keepAliveTimeInMinutes;
        private readonly bool allowMaximumSizeToDivergeFromCoreSize;
        private readonly int rollingCounterNumberOfBuckets;
        private readonly int rollingCounterBucketSizeInMilliseconds;

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
            this.threadPoolKey = threadPoolKey;
            this.coreSize = coreSize;
            this.maximumSize = maximumSize;
            this.maxQueueSize = maxQueueSize;
            this.queueRejectionThreshold = queueRejectionThreshold;
            this.keepAliveTimeInMinutes = keepAliveTimeInMinutes;
            this.allowMaximumSizeToDivergeFromCoreSize = allowMaximumSizeToDivergeFromCoreSize;
            this.rollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
            this.rollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
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

        public IHystrixThreadPoolKey ThreadPoolKey
        {
            get { return threadPoolKey; }
        }

        public int CoreSize
        {
            get { return coreSize; }
        }

        public int MaximumSize
        {
            get
            {
                if (allowMaximumSizeToDivergeFromCoreSize)
                {
                    return maximumSize;
                }
                else
                {
                    return coreSize;
                }
            }
        }

        public int MaxQueueSize
        {
            get { return maxQueueSize; }
        }

        public int QueueRejectionThreshold
        {
            get { return queueRejectionThreshold; }
        }

        public int KeepAliveTimeInMinutes
        {
            get { return keepAliveTimeInMinutes; }
        }

        public bool AllowMaximumSizeToDivergeFromCoreSize
        {
            get { return allowMaximumSizeToDivergeFromCoreSize; }
        }

        public int RollingCounterNumberOfBuckets
        {
            get { return rollingCounterNumberOfBuckets; }
        }

        public int RollingCounterBucketSizeInMilliseconds
        {
            get { return rollingCounterBucketSizeInMilliseconds; }
        }
    }
}
