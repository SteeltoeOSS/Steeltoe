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
        public HystrixCollapserConfiguration(
            IHystrixCollapserKey collapserKey,
            int maxRequestsInBatch,
            int timerDelayInMilliseconds,
            bool requestCacheEnabled,
            CollapserMetricsConfig collapserMetricsConfig)
        {
            CollapserKey = collapserKey;
            MaxRequestsInBatch = maxRequestsInBatch;
            TimerDelayInMilliseconds = timerDelayInMilliseconds;
            IsRequestCacheEnabled = requestCacheEnabled;
            CollapserMetricsConfiguration = collapserMetricsConfig;
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

        public IHystrixCollapserKey CollapserKey { get; }

        public int MaxRequestsInBatch { get; }

        public int TimerDelayInMilliseconds { get; }

        public bool IsRequestCacheEnabled { get; }

        public CollapserMetricsConfig CollapserMetricsConfiguration { get; }

        public class CollapserMetricsConfig
        {
            public CollapserMetricsConfig(
                int rollingPercentileNumberOfBuckets,
                int rollingPercentileBucketSizeInMilliseconds,
                bool rollingPercentileEnabled,
                int rollingCounterNumberOfBuckets,
                int rollingCounterBucketSizeInMilliseconds)
            {
                RollingPercentileNumberOfBuckets = rollingCounterNumberOfBuckets;
                RollingPercentileBucketSizeInMilliseconds = rollingPercentileBucketSizeInMilliseconds;
                IsRollingPercentileEnabled = rollingPercentileEnabled;
                RollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
                RollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
            }

            public int RollingPercentileNumberOfBuckets { get; }

            public int RollingPercentileBucketSizeInMilliseconds { get; }

            public bool IsRollingPercentileEnabled { get; }

            public int RollingCounterNumberOfBuckets { get; }

            public int RollingCounterBucketSizeInMilliseconds { get; }
        }
    }
}
