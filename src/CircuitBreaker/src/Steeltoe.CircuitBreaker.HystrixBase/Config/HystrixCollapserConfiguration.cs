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

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixCollapserConfiguration
    {
        private readonly IHystrixCollapserKey collapserKey;
        private readonly int maxRequestsInBatch;
        private readonly int timerDelayInMilliseconds;
        private readonly bool requestCacheEnabled;
        private readonly CollapserMetricsConfig collapserMetricsConfig;

        public HystrixCollapserConfiguration(
            IHystrixCollapserKey collapserKey,
            int maxRequestsInBatch,
            int timerDelayInMilliseconds,
            bool requestCacheEnabled,
            CollapserMetricsConfig collapserMetricsConfig)
        {
            this.collapserKey = collapserKey;
            this.maxRequestsInBatch = maxRequestsInBatch;
            this.timerDelayInMilliseconds = timerDelayInMilliseconds;
            this.requestCacheEnabled = requestCacheEnabled;
            this.collapserMetricsConfig = collapserMetricsConfig;
        }

        public static HystrixCollapserConfiguration Sample(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions collapserProperties)
        {
            CollapserMetricsConfig collapserMetricsConfig = new CollapserMetricsConfig(
                    collapserProperties.MetricsRollingPercentileWindowBuckets,
                    collapserProperties.MetricsRollingPercentileWindowInMilliseconds,
                    collapserProperties.MetricsRollingPercentileEnabled,
                    collapserProperties.MetricsRollingStatisticalWindowBuckets,
                    collapserProperties.MetricsRollingStatisticalWindowInMilliseconds);

            return new HystrixCollapserConfiguration(
                    collapserKey,
                    collapserProperties.MaxRequestsInBatch,
                    collapserProperties.TimerDelayInMilliseconds,
                    collapserProperties.RequestCacheEnabled,
                    collapserMetricsConfig);
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return collapserKey; }
        }

        public int MaxRequestsInBatch
        {
            get { return maxRequestsInBatch; }
        }

        public int TimerDelayInMilliseconds
        {
            get { return timerDelayInMilliseconds; }
        }

        public bool IsRequestCacheEnabled
        {
            get { return requestCacheEnabled; }
        }

        public CollapserMetricsConfig CollapserMetricsConfiguration
        {
            get { return collapserMetricsConfig; }
        }

        public class CollapserMetricsConfig
        {
            private readonly int rollingPercentileNumberOfBuckets;
            private readonly int rollingPercentileBucketSizeInMilliseconds;
            private readonly bool rollingPercentileEnabled;
            private readonly int rollingCounterNumberOfBuckets;
            private readonly int rollingCounterBucketSizeInMilliseconds;

            public CollapserMetricsConfig(
                int rollingPercentileNumberOfBuckets,
                int rollingPercentileBucketSizeInMilliseconds,
                bool rollingPercentileEnabled,
                int rollingCounterNumberOfBuckets,
                int rollingCounterBucketSizeInMilliseconds)
            {
                this.rollingPercentileNumberOfBuckets = rollingCounterNumberOfBuckets;
                this.rollingPercentileBucketSizeInMilliseconds = rollingPercentileBucketSizeInMilliseconds;
                this.rollingPercentileEnabled = rollingPercentileEnabled;
                this.rollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
                this.rollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
            }

            public int RollingPercentileNumberOfBuckets
            {
                get { return rollingPercentileNumberOfBuckets; }
            }

            public int RollingPercentileBucketSizeInMilliseconds
            {
                get { return rollingPercentileBucketSizeInMilliseconds; }
            }

            public bool IsRollingPercentileEnabled
            {
                get { return rollingPercentileEnabled; }
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
}
