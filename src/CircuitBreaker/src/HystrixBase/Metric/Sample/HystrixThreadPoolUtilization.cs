// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixThreadPoolUtilization
    {
        private readonly int _currentActiveCount;
        private readonly int _currentCorePoolSize;
        private readonly int _currentPoolSize;
        private readonly int _currentQueueSize;

        public HystrixThreadPoolUtilization(int currentActiveCount, int currentCorePoolSize, int currentPoolSize, int currentQueueSize)
        {
            this._currentActiveCount = currentActiveCount;
            this._currentCorePoolSize = currentCorePoolSize;
            this._currentPoolSize = currentPoolSize;
            this._currentQueueSize = currentQueueSize;
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
            get { return _currentActiveCount; }
        }

        public int CurrentCorePoolSize
        {
            get { return _currentCorePoolSize; }
        }

        public int CurrentPoolSize
        {
            get { return _currentPoolSize; }
        }

        public int CurrentQueueSize
        {
            get { return _currentQueueSize; }
        }
    }
}
