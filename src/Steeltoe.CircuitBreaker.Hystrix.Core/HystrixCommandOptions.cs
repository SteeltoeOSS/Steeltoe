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

    public enum ExecutionIsolationStrategy
    {
        THREAD,
        SEMAPHORE
    }

    public class HystrixCommandOptions : HystrixBaseOptions, IHystrixCommandOptions
    {
        protected const string HYSTRIX_COMMAND_PREFIX = "hystrix:command";

        protected IHystrixCommandOptions defaults = null;

        internal const int Default_MetricsRollingStatisticalWindow = 10000; // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
        internal const int Default_MetricsRollingStatisticalWindowBuckets = 10; // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second
        internal const int Default_CircuitBreakerRequestVolumeThreshold = 20; // default => statisticalWindowVolumeThreshold: 20 requests in 10 seconds must occur before statistics matter
        internal const int Default_CircuitBreakerSleepWindowInMilliseconds = 5000; // default => sleepWindow: 5000 = 5 seconds that we will sleep before trying again after tripping the circuit
        internal const int Default_CircuitBreakerErrorThresholdPercentage = 50; // default => errorThresholdPercentage = 50 = if 50%+ of requests in 10 seconds are failures or latent then we will trip the circuit
        internal const bool Default_CircuitBreakerForceOpen = false; // default => forceCircuitOpen = false (we want to allow traffic)
                                                                     /* package */
        internal const bool Default_CircuitBreakerForceClosed = false; // default => ignoreErrors = false
        internal const int Default_ExecutionTimeoutInMilliseconds = 1000; // default => executionTimeoutInMilliseconds: 1000 = 1 second
        internal const bool Default_ExecutionTimeoutEnabled = true;
        internal const ExecutionIsolationStrategy Default_IsolationStrategy = ExecutionIsolationStrategy.THREAD;
        internal const bool Default_MetricsRollingPercentileEnabled = true;
        internal const bool Default_RequestCacheEnabled = true;
        internal const int Default_FallbackIsolationSemaphoreMaxConcurrentRequests = 10;
        internal const bool Default_FallbackEnabled = true;
        internal const int Default_ExecutionIsolationSemaphoreMaxConcurrentRequests = 10;
        internal const bool Default_RequestLogEnabled = true;
        internal const bool Default_CircuitBreakerEnabled = true;
        internal const int Default_MetricsRollingPercentileWindow = 60000; // default to 1 minute for RollingPercentile
        internal const int Default_MetricsRollingPercentileWindowBuckets = 6; // default to 6 buckets (10 seconds each in 60 second window)
        internal const int Default_MetricsRollingPercentileBucketSize = 100; // default to 100 values max per bucket
        internal const int Default_MetricsHealthSnapshotIntervalInMilliseconds = 500; // default to 500ms as max frequency between allowing snapshots of health (error percentage etc)


        internal HystrixCommandOptions(IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : base(dynamic)
        {
            this.defaults = defaults;
            CommandKey = null;
            CircuitBreakerEnabled = Default_CircuitBreakerEnabled;
            CircuitBreakerRequestVolumeThreshold = Default_CircuitBreakerRequestVolumeThreshold;
            CircuitBreakerSleepWindowInMilliseconds = Default_CircuitBreakerSleepWindowInMilliseconds;
            CircuitBreakerErrorThresholdPercentage = Default_CircuitBreakerErrorThresholdPercentage;
            CircuitBreakerForceOpen = Default_CircuitBreakerForceOpen;
            CircuitBreakerForceClosed = Default_CircuitBreakerForceClosed;
            ExecutionIsolationStrategy = Default_IsolationStrategy;

            ExecutionTimeoutInMilliseconds = Default_ExecutionTimeoutInMilliseconds;
            ExecutionTimeoutEnabled = Default_ExecutionTimeoutEnabled;
            ExecutionIsolationSemaphoreMaxConcurrentRequests = Default_ExecutionIsolationSemaphoreMaxConcurrentRequests;
            FallbackIsolationSemaphoreMaxConcurrentRequests = Default_FallbackIsolationSemaphoreMaxConcurrentRequests;
            FallbackEnabled = Default_FallbackEnabled;
            MetricsRollingStatisticalWindowInMilliseconds = Default_MetricsRollingStatisticalWindow;
            MetricsRollingStatisticalWindowBuckets = Default_MetricsRollingStatisticalWindowBuckets;
            MetricsRollingPercentileEnabled = Default_MetricsRollingPercentileEnabled;
            MetricsRollingPercentileWindowInMilliseconds = Default_MetricsRollingPercentileWindow;
            MetricsRollingPercentileWindowBuckets = Default_MetricsRollingPercentileWindowBuckets;
            MetricsRollingPercentileBucketSize = Default_MetricsRollingPercentileBucketSize;
            MetricsHealthSnapshotIntervalInMilliseconds = Default_MetricsHealthSnapshotIntervalInMilliseconds;
            RequestCacheEnabled = Default_RequestCacheEnabled;
            RequestLogEnabled = Default_RequestLogEnabled;
        }

        public HystrixCommandOptions(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
            : this(key, defaults, dynamic)
        {
            GroupKey = groupKey;
        }

        public HystrixCommandOptions(IHystrixCommandKey key, IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : this(defaults, dynamic)
        {
            CommandKey = key;
            CircuitBreakerEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker.enabled", Default_CircuitBreakerEnabled, defaults?.CircuitBreakerEnabled);
            CircuitBreakerRequestVolumeThreshold = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:requestVolumeThreshold", Default_CircuitBreakerRequestVolumeThreshold, defaults?.CircuitBreakerRequestVolumeThreshold);
            CircuitBreakerSleepWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:sleepWindowInMilliseconds", Default_CircuitBreakerSleepWindowInMilliseconds, defaults?.CircuitBreakerSleepWindowInMilliseconds);
            CircuitBreakerErrorThresholdPercentage = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:errorThresholdPercentage", Default_CircuitBreakerErrorThresholdPercentage, defaults?.CircuitBreakerErrorThresholdPercentage);
            CircuitBreakerForceOpen = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:forceOpen", Default_CircuitBreakerForceOpen, defaults?.CircuitBreakerForceOpen);
            CircuitBreakerForceClosed = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:forceClosed", Default_CircuitBreakerForceClosed, defaults?.CircuitBreakerForceClosed);
            ExecutionIsolationStrategy = GetIsolationStrategy(key);
            ExecutionTimeoutInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:isolation:thread:timeoutInMilliseconds", Default_ExecutionTimeoutInMilliseconds, defaults?.ExecutionTimeoutInMilliseconds);
            ExecutionTimeoutEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:timeout:enabled", Default_ExecutionTimeoutEnabled, defaults?.ExecutionTimeoutEnabled);
            ExecutionIsolationSemaphoreMaxConcurrentRequests = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:isolation:semaphore:maxConcurrentRequests", Default_ExecutionIsolationSemaphoreMaxConcurrentRequests, defaults?.ExecutionIsolationSemaphoreMaxConcurrentRequests);
            FallbackIsolationSemaphoreMaxConcurrentRequests = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "fallback:isolation:semaphore:maxConcurrentRequests", Default_FallbackIsolationSemaphoreMaxConcurrentRequests, defaults?.FallbackIsolationSemaphoreMaxConcurrentRequests);
            FallbackEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "fallback:enabled", Default_FallbackEnabled, defaults?.FallbackEnabled);
            MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingStats:timeInMilliseconds", MetricsRollingStatisticalWindowInMilliseconds, defaults?.MetricsRollingStatisticalWindowInMilliseconds);
            MetricsRollingStatisticalWindowBuckets = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingStats:numBuckets", Default_MetricsRollingStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);
            MetricsRollingPercentileEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:enabled", Default_MetricsRollingPercentileEnabled, defaults?.MetricsRollingPercentileEnabled);
            MetricsRollingPercentileWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:timeInMilliseconds", Default_MetricsRollingPercentileWindow, defaults?.MetricsRollingPercentileWindow);
            MetricsRollingPercentileWindowBuckets = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:numBuckets", Default_MetricsRollingPercentileWindowBuckets, defaults?.MetricsRollingPercentileWindowBuckets);
            MetricsRollingPercentileBucketSize = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:bucketSize", Default_MetricsRollingPercentileBucketSize, defaults?.MetricsRollingPercentileBucketSize);
            MetricsHealthSnapshotIntervalInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:healthSnapshot:intervalInMilliseconds", Default_MetricsHealthSnapshotIntervalInMilliseconds, defaults?.MetricsHealthSnapshotIntervalInMilliseconds);
            RequestCacheEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "requestCache:enabled", Default_RequestCacheEnabled, defaults?.RequestCacheEnabled);
            RequestLogEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "requestLog:enabled", Default_RequestLogEnabled, defaults?.RequestLogEnabled);
        }


        public IHystrixCommandGroupKey GroupKey { get; set; }

        public IHystrixCommandKey CommandKey { get; set; }

        public IHystrixThreadPoolKey ThreadPoolKey { get; set; }

        /// <summary>
        /// Whether to use a <seealso cref="HystrixCircuitBreaker"/> or not. If false no circuit-breaker logic will be used and all requests permitted.
        /// <para>
        /// This is similar in effect to <seealso cref="#circuitBreakerForceClosed"/> except that continues tracking metrics and knowing whether it
        /// should be open/closed, this property results in not even instantiating a circuit-breaker.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool CircuitBreakerEnabled { get; set; }

        /// <summary>
        /// Error percentage threshold (as whole number such as 50) at which point the circuit breaker will trip open and reject requests.
        /// <para>
        /// It will stay tripped for the duration defined in <seealso cref="#circuitBreakerSleepWindowInMilliseconds()"/>;
        /// </para>
        /// <para>
        /// The error percentage this is compared against comes from <seealso cref="HystrixCommandMetrics#getHealthCounts()"/>.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int CircuitBreakerErrorThresholdPercentage { get; set; }


        /// <summary>
        /// If true the <seealso cref="HystrixCircuitBreaker#allowRequest()"/> will always return true to allow requests regardless of the error percentage from <seealso cref="HystrixCommandMetrics#getHealthCounts()"/>.
        /// <para>
        /// The <seealso cref="#circuitBreakerForceOpen()"/> property takes precedence so if it set to true this property does nothing.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool CircuitBreakerForceClosed { get; set; }

        /// <summary>
        /// If true the <seealso cref="HystrixCircuitBreaker#allowRequest()"/> will always return false, causing the circuit to be open (tripped) and reject all requests.
        /// <para>
        /// This property takes precedence over <seealso cref="#circuitBreakerForceClosed()"/>;
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool CircuitBreakerForceOpen { get; set; }

        /// <summary>
        /// Minimum number of requests in the <seealso cref="#metricsRollingStatisticalWindowInMilliseconds()"/> that must exist before the <seealso cref="HystrixCircuitBreaker"/> will trip.
        /// <para>
        /// If below this number the circuit will not trip regardless of error percentage.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int CircuitBreakerRequestVolumeThreshold { get; set; }

        /// <summary>
        /// The time in milliseconds after a <seealso cref="HystrixCircuitBreaker"/> trips open that it should wait before trying requests again.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int CircuitBreakerSleepWindowInMilliseconds { get; set; }

        /// <summary>
        /// Number of concurrent requests permitted to <seealso cref="HystrixCommand#run()"/>. Requests beyond the concurrent limit will be rejected.
        /// <para>
        /// Applicable only when <seealso cref="#executionIsolationStrategy()"/> == SEMAPHORE.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int ExecutionIsolationSemaphoreMaxConcurrentRequests { get; set; }

        /// <summary>
        /// What isolation strategy <seealso cref="HystrixCommand#run()"/> will be executed with.
        /// <para>
        /// If <seealso cref="ExecutionIsolationStrategy#THREAD"/> then it will be executed on a separate thread and concurrent requests limited by the number of threads in the thread-pool.
        /// </para>
        /// <para>
        /// If <seealso cref="ExecutionIsolationStrategy#SEMAPHORE"/> then it will be executed on the calling thread and concurrent requests limited by the semaphore count.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual ExecutionIsolationStrategy ExecutionIsolationStrategy { get; set; }


        /// <summary>
        /// Whether the execution thread should attempt an interrupt (using <seealso cref="Future#cancel"/>) when a thread times out.
        /// <para>
        /// Applicable only when <seealso cref="#executionIsolationStrategy()"/> == THREAD.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        //public virtual bool ExecutionIsolationThreadInterruptOnTimeout { get; set; }

        /// <summary>
        /// Whether the execution thread should be interrupted if the execution observable is unsubscribed or the future is cancelled via <seealso cref="Future#cancel(true)"/>).
        /// <para>
        /// Applicable only when <seealso cref="#executionIsolationStrategy()"/> == THREAD.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        //public virtual bool ExecutionIsolationThreadInterruptOnFutureCancel { get; set; }


        /// <summary>
        /// Allow a dynamic override of the <seealso cref="HystrixThreadPoolKey"/> that will dynamically change which <seealso cref="HystrixThreadPool"/> a <seealso cref="HystrixCommand"/> executes on.
        /// <para>
        /// Typically this should return NULL which will cause it to use the <seealso cref="HystrixThreadPoolKey"/> injected into a <seealso cref="HystrixCommand"/> or derived from the <seealso cref="HystrixCommandGroupKey"/>.
        /// </para>
        /// <para>
        /// When set the injected or derived values will be ignored and a new <seealso cref="HystrixThreadPool"/> created (if necessary) and the <seealso cref="HystrixCommand"/> will begin using the newly defined pool.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code String>} </returns>
        public virtual string ExecutionIsolationThreadPoolKeyOverride { get; set; }

        /// <summary>
        /// Time in milliseconds at which point the command will timeout and halt execution.
        /// <para>
        /// If <seealso cref="#executionIsolationThreadInterruptOnTimeout"/> == true and the command is thread-isolated, the executing thread will be interrupted.
        /// If the command is semaphore-isolated and a <seealso cref="HystrixObservableCommand"/>, that command will get unsubscribed.
        /// </para>
        /// <para>
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int ExecutionTimeoutInMilliseconds { get; set; }


        /// <summary>
        /// Whether the timeout mechanism is enabled for this command
        /// </summary>
        /// <returns> {@code Boolean>}
        /// 
        /// @since 1.4.4 </returns>
        public virtual bool ExecutionTimeoutEnabled { get; set; }


        /// <summary>
        /// Number of concurrent requests permitted to <seealso cref="HystrixCommand#getFallback()"/>. Requests beyond the concurrent limit will fail-fast and not attempt retrieving a fallback.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int FallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }


        /// <summary>
        /// Whether <seealso cref="HystrixCommand#getFallback()"/> should be attempted when failure occurs.
        /// </summary>
        /// <returns> {@code Boolean>}
        /// 
        /// @since 1.2 </returns>
        public virtual bool FallbackEnabled { get; set; }

        /// <summary>
        /// Time in milliseconds to wait between allowing health snapshots to be taken that calculate success and error percentages and affect <seealso cref="HystrixCircuitBreaker#isOpen()"/> status.
        /// <para>
        /// On high-volume circuits the continual calculation of error percentage can become CPU intensive thus this controls how often it is calculated.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsHealthSnapshotIntervalInMilliseconds { get; set; }


        /// <summary>
        /// Maximum number of values stored in each bucket of the rolling percentile. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsRollingPercentileBucketSize { get; set; }

        /// <summary>
        /// Whether percentile metrics should be captured using <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool MetricsRollingPercentileEnabled { get; set; }

        /// <summary>
        /// Duration of percentile rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        /// @deprecated Use <seealso cref="#metricsRollingPercentileWindowInMilliseconds()"/> 
        public virtual int MetricsRollingPercentileWindow { get; set; }

        /// <summary>
        /// Duration of percentile rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsRollingPercentileWindowInMilliseconds { get; set; }


        /// <summary>
        /// Number of buckets the rolling percentile window is broken into. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsRollingPercentileWindowBuckets { get; set; }
        /// <summary>
        /// Duration of statistical rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingNumber"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsRollingStatisticalWindowInMilliseconds { get; set; }


        /// <summary>
        /// Number of buckets the rolling statistical window is broken into. This is passed into <seealso cref="HystrixRollingNumber"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        public virtual int MetricsRollingStatisticalWindowBuckets { get; set; }

        /// <summary>
        /// Whether <seealso cref="HystrixCommand#getCacheKey()"/> should be used with <seealso cref="HystrixRequestCache"/> to provide de-duplication functionality via request-scoped caching.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool RequestCacheEnabled { get; set; }

        /// <summary>
        /// Whether <seealso cref="HystrixCommand"/> execution and events should be logged to <seealso cref="HystrixRequestLog"/>.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        public virtual bool RequestLogEnabled { get; set; }

        protected virtual ExecutionIsolationStrategy GetIsolationStrategy(IHystrixCommandKey key)
        {
            var isolation = GetString(HYSTRIX_COMMAND_PREFIX, key.Name, "execution.isolation.strategy", Default_IsolationStrategy.ToString(), defaults?.ExecutionIsolationStrategy.ToString());
            if (ExecutionIsolationStrategy.THREAD.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionIsolationStrategy.THREAD;
            }
            if (ExecutionIsolationStrategy.SEMAPHORE.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
            {
                return ExecutionIsolationStrategy.SEMAPHORE;
            }
            throw new ArgumentOutOfRangeException("execution.isolation.strategy");
        }

        public virtual IHystrixThreadPoolOptions ThreadPoolOptions { get; set; }
    }
}
