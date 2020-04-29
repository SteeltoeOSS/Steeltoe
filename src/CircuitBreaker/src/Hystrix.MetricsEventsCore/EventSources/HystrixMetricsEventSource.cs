using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources
{
    [EventSource(Name ="Steeltoe.Hystrix.Events")]
    public class HystrixMetricsEventSource : EventSource, IHystrixEventSource
    {
        public class Keywords
        {
            public const EventKeywords Command = (EventKeywords)1;
        }

        private static readonly Lazy<HystrixMetricsEventSource> Instance
            = new Lazy<HystrixMetricsEventSource>(() => new HystrixMetricsEventSource());

        private HystrixMetricsEventSource()
        {
        }

        public static HystrixMetricsEventSource EventLogger
        {
            get { return Instance.Value; }
        }

        [Event(1, Message = "CommandEvent", Level = EventLevel.Verbose, Keywords = Keywords.Command)]
        public void ExecutionCount(long executionCount)
        {
            if (IsEnabled())
            {
                WriteEvent(1, executionCount);
            }
        }

        [Event(2, Message = "CommandEvent2", Level = EventLevel.Verbose, Keywords = Keywords.Command)]

        public void CommandMetrics(
            string commandKey,
            string commandGroup,
            bool isCiruitBreakerOpen,
            long errorPercent,
            long errorCount,
            long requestCount,
            long rollingCountBadRequests,
            long rollingCountCollapsedRequests,
            long rollingCountEmit,
            long rollingCountExceptionsThrown,
            long rollingCountFailure,
            long rollingCountFallbackEmit,
            long rollingCountFallbackFailure,
            long rollingCountFallbackMissing,
            long rollingCountFallbackRejection,
            long rollingCountFallbackSuccess,
            long rollingCountResponsesFromCache,
            long rollingCountSemaphoreRejected,
            long rollingCountShortCircuited,
            long rollingCountSuccess,
            long rollingCountThreadPoolRejected,
            long rollingCountTimeout,
            int currentConcurrentExecutionCount,
            long rollingMaxConcurrentExecutionCount,
            int latencyExecute_mean,
            int latencyTotal_mean,
            int propertyValue_circuitBreakerRequestVolumeThreshold,
            int propertyValue_circuitBreakerSleepWindowInMilliseconds,
            int propertyValue_circuitBreakerErrorThresholdPercentage,
            bool propertyValue_circuitBreakerForceOpen,
            bool propertyValue_circuitBreakerForceClosed,
            bool propertyValue_circuitBreakerEnabled,
            string propertyValue_executionIsolationStrategy,
            int propertyValue_executionIsolationThreadTimeoutInMilliseconds,
            int propertyValue_executionTimeoutInMilliseconds,
            bool propertyValue_executionIsolationThreadInterruptOnTimeout,
            string propertyValue_executionIsolationThreadPoolKeyOverride,
            int propertyValue_executionIsolationSemaphoreMaxConcurrentRequests,
            int propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests,
            int propertyValue_metricsRollingStatisticalWindowInMilliseconds,
            bool propertyValue_requestCacheEnabled,
            bool propertyValue_requestLogEnabled,
            int reportingHosts,
            string threadPool)
        {
            if (IsEnabled())
            {
                WriteEvent(
                    2,
                    commandKey,
                    commandGroup,
                    isCiruitBreakerOpen,
                    errorPercent,
                    errorCount,
                    requestCount,
                    rollingCountBadRequests,
                    rollingCountCollapsedRequests,
                    rollingCountEmit,
                    rollingCountExceptionsThrown,
                    rollingCountFailure,
                    rollingCountFallbackEmit,
                    rollingCountFallbackFailure,
                    rollingCountFallbackMissing,
                    rollingCountFallbackRejection,
                    rollingCountFallbackSuccess,
                    rollingCountResponsesFromCache,
                    rollingCountSemaphoreRejected,
                    rollingCountShortCircuited,
                    rollingCountSuccess,
                    rollingCountThreadPoolRejected,
                    rollingCountTimeout,
                    currentConcurrentExecutionCount,
                    rollingMaxConcurrentExecutionCount,
                    latencyExecute_mean,
                    latencyTotal_mean,
                    propertyValue_circuitBreakerRequestVolumeThreshold,
                    propertyValue_circuitBreakerSleepWindowInMilliseconds,
                    propertyValue_circuitBreakerErrorThresholdPercentage,
                    propertyValue_circuitBreakerForceOpen,
                    propertyValue_circuitBreakerForceClosed,
                    propertyValue_circuitBreakerEnabled,
                    propertyValue_executionIsolationStrategy,
                    propertyValue_executionIsolationThreadTimeoutInMilliseconds,
                    propertyValue_executionTimeoutInMilliseconds,
                    propertyValue_executionIsolationThreadInterruptOnTimeout,
                    propertyValue_executionIsolationThreadPoolKeyOverride,
                    propertyValue_executionIsolationSemaphoreMaxConcurrentRequests,
                    propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests,
                    propertyValue_metricsRollingStatisticalWindowInMilliseconds,
                    propertyValue_requestCacheEnabled,
                    propertyValue_requestLogEnabled,
                    reportingHosts,
                    threadPool);
            }
        }
    }
}
