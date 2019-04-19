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
