// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixThreadPoolUtilization
    {
        private readonly int currentActiveCount;
        private readonly int currentCorePoolSize;
        private readonly int currentPoolSize;
        private readonly int currentQueueSize;

        public HystrixThreadPoolUtilization(int currentActiveCount, int currentCorePoolSize, int currentPoolSize, int currentQueueSize)
        {
            this.currentActiveCount = currentActiveCount;
            this.currentCorePoolSize = currentCorePoolSize;
            this.currentPoolSize = currentPoolSize;
            this.currentQueueSize = currentQueueSize;
        }

        public static HystrixThreadPoolUtilization Sample(HystrixThreadPoolMetrics threadPoolMetrics)
        {
            return new HystrixThreadPoolUtilization(
                    threadPoolMetrics.CurrentActiveCount,
                    threadPoolMetrics.CurrentCorePoolSize,
                    threadPoolMetrics.CurrentPoolSize,
                    threadPoolMetrics.CurrentQueueSize);
        }

        public int CurrentActiveCount
        {
            get { return currentActiveCount; }
        }

        public int CurrentCorePoolSize
        {
            get { return currentCorePoolSize; }
        }

        public int CurrentPoolSize
        {
            get { return currentPoolSize; }
        }

        public int CurrentQueueSize
        {
            get { return currentQueueSize; }
        }
    }
}
