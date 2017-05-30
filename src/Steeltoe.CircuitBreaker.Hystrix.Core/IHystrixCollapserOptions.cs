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


namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixCollapserOptions
    {

        IHystrixCollapserKey CollapserKey { get; set; }

        RequestCollapserScope Scope { get; set; }

        /**
        * Whether request caching is enabled for {@link HystrixCollapser#execute} and {@link HystrixCollapser#queue} invocations.
        *
        * @return {@code HystrixProperty<Boolean>}
        */
        bool RequestCacheEnabled { get; set; }

        /**
         * The maximum number of requests allowed in a batch before triggering a batch execution.
         * 
         * @return {@code HystrixProperty<Integer>}
         */
        int MaxRequestsInBatch { get; set; }

        /**
         * The number of milliseconds between batch executions (unless {@link #maxRequestsInBatch} is hit which will cause a batch to execute early.
         * 
         * @return {@code HystrixProperty<Integer>}
         */
        int TimerDelayInMilliseconds { get; set; }


        /**
         * Duration of statistical rolling window in milliseconds. This is passed into {@link HystrixRollingNumber} inside {@link HystrixCommandMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        int MetricsRollingStatisticalWindowInMilliseconds { get; set; }


        /**
         * Number of buckets the rolling statistical window is broken into. This is passed into {@link HystrixRollingNumber} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        int MetricsRollingStatisticalWindowBuckets { get; set; }


        /**
         * Whether percentile metrics should be captured using {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Boolean>}
         */
        bool MetricsRollingPercentileEnabled { get; set; }


        /**
         * Duration of percentile rolling window in milliseconds. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        int MetricsRollingPercentileWindowInMilliseconds { get; set; }

        /**
         * Number of buckets the rolling percentile window is broken into. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        int MetricsRollingPercentileWindowBuckets { get; set; }


        /**
         * Maximum number of values stored in each bucket of the rolling percentile. This is passed into {@link HystrixRollingPercentile} inside {@link HystrixCollapserMetrics}.
         *
         * @return {@code HystrixProperty<Integer>}
         */
        int MetricsRollingPercentileBucketSize { get; set; }
    }
}
