// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixCollapserOptions : HystrixBaseOptions, IHystrixCollapserOptions
    {
        internal const int DEFAULT_MAX_REQUESTS_IN_BATCH = int.MaxValue;
        internal const int DEFAULT_TIMER_DELAY_IN_MILLISECONDS = 10;
        internal const bool DEFAULT_REQUEST_CACHE_ENABLED = true;
        internal const int DEFAULT_METRICS_ROLLING_STATISTICAL_WINDOW = 10000; // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
        internal const int DEFAULT_METRICS_ROLLING_STATISTICAL_WINDOW_BUCKETS = 10; // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second
        internal const bool DEFAULT_METRICS_ROLLING_PERCENTILE_ENABLED = true;
        internal const int DEFAULT_METRICS_ROLLING_PERCENTILE_WINDOW = 60000; // default to 1 minute for RollingPercentile
        internal const int DEFAULT_METRICS_ROLLING_PERCENTILE_WINDOW_BUCKETS = 6; // default to 6 buckets (10 seconds each in 60 second window)
        internal const int DEFAULT_METRICS_ROLLING_PERCENTILE_BUCKET_SIZE = 100; // default to 100 values max per bucket

        protected const string HYSTRIX_COLLAPSER_PREFIX = "hystrix:collapser";
        private readonly IHystrixCollapserOptions _defaults;

        public HystrixCollapserOptions(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : this(collapserKey, RequestCollapserScope.REQUEST, defaults, dynamic)
        {
        }

        public HystrixCollapserOptions(IHystrixCollapserKey key, RequestCollapserScope scope, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : base(dynamic)
        {
            CollapserKey = key;
            Scope = scope;
            _defaults = defaults;

            MaxRequestsInBatch = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "maxRequestsInBatch", DEFAULT_MAX_REQUESTS_IN_BATCH, defaults?.MaxRequestsInBatch);
            TimerDelayInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "timerDelayInMilliseconds", DEFAULT_TIMER_DELAY_IN_MILLISECONDS, defaults?.TimerDelayInMilliseconds);
            RequestCacheEnabled = GetBoolean(HYSTRIX_COLLAPSER_PREFIX, key.Name, "requestCache.enabled", DEFAULT_REQUEST_CACHE_ENABLED, defaults?.RequestCacheEnabled);
            MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingStats.timeInMilliseconds", DEFAULT_METRICS_ROLLING_STATISTICAL_WINDOW, defaults?.MetricsRollingStatisticalWindowInMilliseconds);
            MetricsRollingStatisticalWindowBuckets = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingStats.numBuckets", DEFAULT_METRICS_ROLLING_STATISTICAL_WINDOW_BUCKETS, defaults?.MetricsRollingStatisticalWindowBuckets);
            MetricsRollingPercentileEnabled = GetBoolean(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.enabled", DEFAULT_METRICS_ROLLING_PERCENTILE_ENABLED, defaults?.MetricsRollingPercentileEnabled);
            MetricsRollingPercentileWindowInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.timeInMilliseconds", DEFAULT_METRICS_ROLLING_PERCENTILE_WINDOW, defaults?.MetricsRollingPercentileWindowInMilliseconds);
            MetricsRollingPercentileWindowBuckets = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.numBuckets", DEFAULT_METRICS_ROLLING_PERCENTILE_WINDOW_BUCKETS, defaults?.MetricsRollingPercentileWindowBuckets);
            MetricsRollingPercentileBucketSize = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.bucketSize", DEFAULT_METRICS_ROLLING_PERCENTILE_BUCKET_SIZE, defaults?.MetricsRollingPercentileBucketSize);
        }

        public IHystrixCollapserKey CollapserKey { get; set; }

        public RequestCollapserScope Scope { get; set; }

        public bool RequestCacheEnabled { get; set; }

        public int MaxRequestsInBatch { get; set; }

        public int TimerDelayInMilliseconds { get; set; }

        public int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

        public int MetricsRollingStatisticalWindowBuckets { get; set; }

        public bool MetricsRollingPercentileEnabled { get; set; }

        public int MetricsRollingPercentileWindowInMilliseconds { get; set; }

        public int MetricsRollingPercentileWindowBuckets { get; set; }

        public int MetricsRollingPercentileBucketSize { get; set; }
    }
}
