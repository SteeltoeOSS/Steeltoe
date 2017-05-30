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
    public interface IHystrixCommandOptions
    {
        IHystrixCommandGroupKey GroupKey { get; set; }

        IHystrixCommandKey CommandKey { get; set; }

        IHystrixThreadPoolKey ThreadPoolKey { get; set; }

        /// <summary>
        /// Whether to use a <seealso cref="HystrixCircuitBreaker"/> or not. If false no circuit-breaker logic will be used and all requests permitted.
        /// <para>
        /// This is similar in effect to <seealso cref="#circuitBreakerForceClosed"/> except that continues tracking metrics and knowing whether it
        /// should be open/closed, this property results in not even instantiating a circuit-breaker.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        bool CircuitBreakerEnabled { get; set; }

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
        int CircuitBreakerErrorThresholdPercentage { get; set; }


        /// <summary>
        /// If true the <seealso cref="HystrixCircuitBreaker#allowRequest()"/> will always return true to allow requests regardless of the error percentage from <seealso cref="HystrixCommandMetrics#getHealthCounts()"/>.
        /// <para>
        /// The <seealso cref="#circuitBreakerForceOpen()"/> property takes precedence so if it set to true this property does nothing.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
       bool CircuitBreakerForceClosed { get; set; }

        /// <summary>
        /// If true the <seealso cref="HystrixCircuitBreaker#allowRequest()"/> will always return false, causing the circuit to be open (tripped) and reject all requests.
        /// <para>
        /// This property takes precedence over <seealso cref="#circuitBreakerForceClosed()"/>;
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        bool CircuitBreakerForceOpen { get; set; }

        /// <summary>
        /// Minimum number of requests in the <seealso cref="#metricsRollingStatisticalWindowInMilliseconds()"/> that must exist before the <seealso cref="HystrixCircuitBreaker"/> will trip.
        /// <para>
        /// If below this number the circuit will not trip regardless of error percentage.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int CircuitBreakerRequestVolumeThreshold { get; set; }

        /// <summary>
        /// The time in milliseconds after a <seealso cref="HystrixCircuitBreaker"/> trips open that it should wait before trying requests again.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int CircuitBreakerSleepWindowInMilliseconds { get; set; }

        /// <summary>
        /// Number of concurrent requests permitted to <seealso cref="HystrixCommand#run()"/>. Requests beyond the concurrent limit will be rejected.
        /// <para>
        /// Applicable only when <seealso cref="#executionIsolationStrategy()"/> == SEMAPHORE.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int ExecutionIsolationSemaphoreMaxConcurrentRequests { get; set; }

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
        ExecutionIsolationStrategy ExecutionIsolationStrategy { get; set; }


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
        string ExecutionIsolationThreadPoolKeyOverride { get; set; }

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
        int ExecutionTimeoutInMilliseconds { get; set; }


        /// <summary>
        /// Whether the timeout mechanism is enabled for this command
        /// </summary>
        /// <returns> {@code Boolean>}
        /// 
        /// @since 1.4.4 </returns>
        bool ExecutionTimeoutEnabled { get; set; }


        /// <summary>
        /// Number of concurrent requests permitted to <seealso cref="HystrixCommand#getFallback()"/>. Requests beyond the concurrent limit will fail-fast and not attempt retrieving a fallback.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int FallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }


        /// <summary>
        /// Whether <seealso cref="HystrixCommand#getFallback()"/> should be attempted when failure occurs.
        /// </summary>
        /// <returns> {@code Boolean>}
        /// 
        /// @since 1.2 </returns>
        bool FallbackEnabled { get; set; }

        /// <summary>
        /// Time in milliseconds to wait between allowing health snapshots to be taken that calculate success and error percentages and affect <seealso cref="HystrixCircuitBreaker#isOpen()"/> status.
        /// <para>
        /// On high-volume circuits the continual calculation of error percentage can become CPU intensive thus this controls how often it is calculated.
        /// 
        /// </para>
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int MetricsHealthSnapshotIntervalInMilliseconds { get; set; }


        /// <summary>
        /// Maximum number of values stored in each bucket of the rolling percentile. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int MetricsRollingPercentileBucketSize { get; set; }

        /// <summary>
        /// Whether percentile metrics should be captured using <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        bool MetricsRollingPercentileEnabled { get; set; }

        /// <summary>
        /// Duration of percentile rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        /// @deprecated Use <seealso cref="#metricsRollingPercentileWindowInMilliseconds()"/> 
        int MetricsRollingPercentileWindow { get; set; }

        /// <summary>
        /// Duration of percentile rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int MetricsRollingPercentileWindowInMilliseconds { get; set; }


        /// <summary>
        /// Number of buckets the rolling percentile window is broken into. This is passed into <seealso cref="HystrixRollingPercentile"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int MetricsRollingPercentileWindowBuckets { get; set; }
        /// <summary>
        /// Duration of statistical rolling window in milliseconds. This is passed into <seealso cref="HystrixRollingNumber"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
        int MetricsRollingStatisticalWindowInMilliseconds { get; set; }


        /// <summary>
        /// Number of buckets the rolling statistical window is broken into. This is passed into <seealso cref="HystrixRollingNumber"/> inside <seealso cref="HystrixCommandMetrics"/>.
        /// </summary>
        /// <returns> {@code Integer>} </returns>
         int MetricsRollingStatisticalWindowBuckets { get; set; }

        /// <summary>
        /// Whether <seealso cref="HystrixCommand#getCacheKey()"/> should be used with <seealso cref="HystrixRequestCache"/> to provide de-duplication functionality via request-scoped caching.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        bool RequestCacheEnabled { get; set; }

        /// <summary>
        /// Whether <seealso cref="HystrixCommand"/> execution and events should be logged to <seealso cref="HystrixRequestLog"/>.
        /// </summary>
        /// <returns> {@code Boolean>} </returns>
        bool RequestLogEnabled { get; set; }
    }
}
