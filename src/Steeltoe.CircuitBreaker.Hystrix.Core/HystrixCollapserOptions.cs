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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixCollapserOptions : HystrixBaseOptions, IHystrixCollapserOptions
    {
        protected const string HYSTRIX_COLLAPSER_PREFIX = "hystrix:collapser";

        internal const int default_maxRequestsInBatch = Int32.MaxValue;
        internal const int default_timerDelayInMilliseconds = 10;
        internal const bool default_requestCacheEnabled = true;
        internal const int default_metricsRollingStatisticalWindow = 10000;// default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
        internal const int default_metricsRollingStatisticalWindowBuckets = 10;// default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second
        internal const bool default_metricsRollingPercentileEnabled = true;
        internal const int default_metricsRollingPercentileWindow = 60000; // default to 1 minute for RollingPercentile
        internal const int default_metricsRollingPercentileWindowBuckets = 6; // default to 6 buckets (10 seconds each in 60 second window)
        internal const int default_metricsRollingPercentileBucketSize = 100; // default to 100 values max per bucket
        private IHystrixCollapserOptions defaults;

        public HystrixCollapserOptions(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : this(collapserKey, RequestCollapserScope.REQUEST, defaults, dynamic)
        { 
        }


        public HystrixCollapserOptions(IHystrixCollapserKey key, RequestCollapserScope scope, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            :base(dynamic)
        {
            this.CollapserKey = key;
            this.Scope = scope;
            this.defaults = defaults;

            this.MaxRequestsInBatch = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "maxRequestsInBatch", default_maxRequestsInBatch, defaults?.MaxRequestsInBatch); 
            this.TimerDelayInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "timerDelayInMilliseconds", default_timerDelayInMilliseconds, defaults?.TimerDelayInMilliseconds); 
            this.RequestCacheEnabled = GetBoolean(HYSTRIX_COLLAPSER_PREFIX, key.Name, "requestCache.enabled", default_requestCacheEnabled, defaults?.RequestCacheEnabled); 
            this.MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingStats.timeInMilliseconds", default_metricsRollingStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds); 
            this.MetricsRollingStatisticalWindowBuckets = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingStats.numBuckets", default_metricsRollingStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets); 
            this.MetricsRollingPercentileEnabled = GetBoolean(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.enabled", default_metricsRollingPercentileEnabled, defaults?.MetricsRollingPercentileEnabled); 
            this.MetricsRollingPercentileWindowInMilliseconds = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.timeInMilliseconds", default_metricsRollingPercentileWindow, defaults?.MetricsRollingPercentileWindowInMilliseconds); 
            this.MetricsRollingPercentileWindowBuckets = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.numBuckets", default_metricsRollingPercentileWindowBuckets, defaults?.MetricsRollingPercentileWindowBuckets); 
            this.MetricsRollingPercentileBucketSize = GetInteger(HYSTRIX_COLLAPSER_PREFIX, key.Name, "metrics.rollingPercentile.bucketSize", default_metricsRollingPercentileBucketSize, defaults?.MetricsRollingPercentileBucketSize); 
        }


        public IHystrixCollapserKey CollapserKey { get; set; }

        public RequestCollapserScope Scope { get; set; }

        /**
        * Whether request caching is enabled for {@link HystrixCollapser#execute} and {@link HystrixCollapser#queue} invocations.
        *
        * @return {@code HystrixProperty<Boolean>}
        */
        public bool RequestCacheEnabled { get; set; }

        /**
         * The maximum number of requests allowed in a batch before triggering a batch execution.
         * 
         * @return {@code HystrixProperty<Integer>}
         */
        public int MaxRequestsInBatch { get; set; }

        /**
         * The number of milliseconds between batch executions (unless {@link #maxRequestsInBatch} is hit which will cause a batch to execute early.
         * 
         * @return {@code HystrixProperty<Integer>}
         */
        public int TimerDelayInMilliseconds { get; set; }


        /**
         * Duration of statistical rolling window in milliseconds. This is passed into {@link HystrixRollingNumber} inside {@link HystrixCommandMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        public int MetricsRollingStatisticalWindowInMilliseconds { get; set; }


        /**
         * Number of buckets the rolling statistical window is broken into. This is passed into {@link HystrixRollingNumber} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        public int MetricsRollingStatisticalWindowBuckets { get; set; }


        /**
         * Whether percentile metrics should be captured using {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Boolean>}
         */
        public bool MetricsRollingPercentileEnabled { get; set; }


        /**
         * Duration of percentile rolling window in milliseconds. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        public int MetricsRollingPercentileWindowInMilliseconds { get; set; }

        /**
         * Number of buckets the rolling percentile window is broken into. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        public int MetricsRollingPercentileWindowBuckets { get; set; }


        /**
         * Maximum number of values stored in each bucket of the rolling percentile. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        public int MetricsRollingPercentileBucketSize { get; set; }

    }
}
