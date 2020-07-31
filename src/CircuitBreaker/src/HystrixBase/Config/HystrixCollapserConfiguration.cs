// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var collapserMetricsConfig = new CollapserMetricsConfig(
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
