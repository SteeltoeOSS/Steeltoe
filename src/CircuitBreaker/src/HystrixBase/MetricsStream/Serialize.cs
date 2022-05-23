// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Serial;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Util;
using Steeltoe.Discovery;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public static class Serialize
    {
        public static List<string> ToJsonList(HystrixDashboardStream.DashboardData data, IDiscoveryClient discoveryClient)
        {
            var jsonList = new List<string>();
            WriteCommandData(data, discoveryClient, jsonList);
            WriteThreadPoolData(data, discoveryClient, jsonList);
            return jsonList;
        }

        private static readonly string ContextId = Guid.NewGuid().ToString();

        private static void WriteLocalService(JsonTextWriter writer, IServiceInstance localService)
        {
            writer.WriteObjectFieldStart("origin");
            writer.WriteStringField("host", localService?.Host);
            if (localService == null)
            {
                writer.WriteIntegerField("port", -1);
            }
            else
            {
                writer.WriteIntegerField("port", localService.Port);
            }

            writer.WriteStringField("serviceId", localService?.ServiceId);
            writer.WriteStringField("id", ContextId);
            writer.WriteEndObject();
        }

        private static void WriteThreadPoolData(HystrixDashboardStream.DashboardData data, IDiscoveryClient discoveryClient, List<string> jsonList)
        {
            try
            {
                var localService = discoveryClient?.GetLocalServiceInstance();

                foreach (var threadPoolMetrics in data.ThreadPoolMetrics)
                {
                    using var sw = new StringWriter();
                    using (var writer = new JsonTextWriter(sw))
                    {
                        writer.WriteStartObject();
                        WriteLocalService(writer, localService);
                        writer.WriteObjectFieldStart("data");
                        WriteThreadPoolMetrics(writer, threadPoolMetrics);
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }

                    jsonList.Add(sw.ToString());
                }
            }
            catch (Exception)
            {
                // Log
            }
        }

        private static void WriteCommandData(HystrixDashboardStream.DashboardData data, IDiscoveryClient discoveryClient, List<string> jsonList)
        {
            try
            {
                var localService = discoveryClient?.GetLocalServiceInstance();

                foreach (var commandMetrics in data.CommandMetrics)
                {
                    using var sw = new StringWriter();
                    using (var writer = new JsonTextWriter(sw))
                    {
                        writer.WriteStartObject();
                        WriteLocalService(writer, localService);
                        writer.WriteObjectFieldStart("data");
                        WriteCommandMetrics(writer, commandMetrics, localService);
                        writer.WriteEndObject();
                        writer.WriteEndObject();
                    }

                    jsonList.Add(sw.ToString());
                }
            }
            catch (Exception)
            {
                // Log
            }
        }

        private static void WriteThreadPoolMetrics(JsonTextWriter writer, HystrixThreadPoolMetrics threadPoolMetrics)
        {
            var key = threadPoolMetrics.ThreadPoolKey;

            writer.WriteStringField("type", "HystrixThreadPool");
            writer.WriteStringField("name", key.Name);
            writer.WriteLongField("currentTime", Time.CurrentTimeMillisJava);

            writer.WriteIntegerField("currentActiveCount", threadPoolMetrics.CurrentActiveCount);
            writer.WriteIntegerField("currentCompletedTaskCount", threadPoolMetrics.CurrentCompletedTaskCount);
            writer.WriteIntegerField("currentCorePoolSize", threadPoolMetrics.CurrentCorePoolSize);
            writer.WriteIntegerField("currentLargestPoolSize", threadPoolMetrics.CurrentLargestPoolSize);
            writer.WriteIntegerField("currentMaximumPoolSize", threadPoolMetrics.CurrentMaximumPoolSize);
            writer.WriteIntegerField("currentPoolSize", threadPoolMetrics.CurrentPoolSize);
            writer.WriteIntegerField("currentQueueSize", threadPoolMetrics.CurrentQueueSize);
            writer.WriteIntegerField("currentTaskCount", threadPoolMetrics.CurrentTaskCount);
            writer.WriteLongField("rollingCountThreadsExecuted", threadPoolMetrics.RollingCountThreadsExecuted);

            writer.WriteLongField("rollingMaxActiveThreads", threadPoolMetrics.RollingMaxActiveThreads);

            writer.WriteIntegerField("propertyValue_queueSizeRejectionThreshold", threadPoolMetrics.Properties.QueueSizeRejectionThreshold);
            writer.WriteIntegerField("propertyValue_metricsRollingStatisticalWindowInMilliseconds", threadPoolMetrics.Properties.MetricsRollingStatisticalWindowInMilliseconds);

            writer.WriteLongField("reportingHosts", 1); // this will get summed across all instances in a cluster
        }

        private static void WriteCommandMetrics(JsonTextWriter writer, HystrixCommandMetrics commandMetrics, IServiceInstance localService)
        {
            var key = commandMetrics.CommandKey;
            var circuitBreaker = HystrixCircuitBreakerFactory.GetInstance(key);

            writer.WriteStringField("type", "HystrixCommand");

            if (localService != null)
            {
                writer.WriteStringField("name", $"{localService.ServiceId}.{key.Name}");
            }
            else
            {
                writer.WriteStringField("name", key.Name);
            }

            writer.WriteStringField("group", commandMetrics.CommandGroup.Name);
            writer.WriteLongField("currentTime", Time.CurrentTimeMillisJava);

            // circuit breaker
            if (circuitBreaker == null)
            {
                // circuit breaker is disabled and thus never open
                writer.WriteBooleanField("isCircuitBreakerOpen", false);
            }
            else
            {
                writer.WriteBooleanField("isCircuitBreakerOpen", circuitBreaker.IsOpen);
            }

            var healthCounts = commandMetrics.Healthcounts;
            writer.WriteIntegerField("errorPercentage", healthCounts.ErrorPercentage);
            writer.WriteLongField("errorCount", healthCounts.ErrorCount);
            writer.WriteLongField("requestCount", healthCounts.TotalRequests);

            // rolling counters
            writer.WriteLongField("rollingCountCollapsedRequests", commandMetrics.GetRollingCount(HystrixEventType.COLLAPSED));
            writer.WriteLongField("rollingCountExceptionsThrown", commandMetrics.GetRollingCount(HystrixEventType.EXCEPTION_THROWN));
            writer.WriteLongField("rollingCountFailure", commandMetrics.GetRollingCount(HystrixEventType.FAILURE));
            writer.WriteLongField("rollingCountFallbackFailure", commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_FAILURE));
            writer.WriteLongField("rollingCountFallbackRejection", commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_REJECTION));
            writer.WriteLongField("rollingCountFallbackSuccess", commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_SUCCESS));
            writer.WriteLongField("rollingCountResponsesFromCache", commandMetrics.GetRollingCount(HystrixEventType.RESPONSE_FROM_CACHE));
            writer.WriteLongField("rollingCountSemaphoreRejected", commandMetrics.GetRollingCount(HystrixEventType.SEMAPHORE_REJECTED));
            writer.WriteLongField("rollingCountShortCircuited", commandMetrics.GetRollingCount(HystrixEventType.SHORT_CIRCUITED));
            writer.WriteLongField("rollingCountSuccess", commandMetrics.GetRollingCount(HystrixEventType.SUCCESS));
            writer.WriteLongField("rollingCountThreadPoolRejected", commandMetrics.GetRollingCount(HystrixEventType.THREAD_POOL_REJECTED));
            writer.WriteLongField("rollingCountTimeout", commandMetrics.GetRollingCount(HystrixEventType.TIMEOUT));

            writer.WriteIntegerField("currentConcurrentExecutionCount", commandMetrics.CurrentConcurrentExecutionCount);

            // latency percentiles
            writer.WriteIntegerField("latencyExecute_mean", commandMetrics.ExecutionTimeMean);
            writer.WriteObjectFieldStart("latencyExecute");
            writer.WriteIntegerField("0", commandMetrics.GetExecutionTimePercentile(0));
            writer.WriteIntegerField("25", commandMetrics.GetExecutionTimePercentile(25));
            writer.WriteIntegerField("50", commandMetrics.GetExecutionTimePercentile(50));
            writer.WriteIntegerField("75", commandMetrics.GetExecutionTimePercentile(75));
            writer.WriteIntegerField("90", commandMetrics.GetExecutionTimePercentile(90));
            writer.WriteIntegerField("95", commandMetrics.GetExecutionTimePercentile(95));
            writer.WriteIntegerField("99", commandMetrics.GetExecutionTimePercentile(99));
            writer.WriteIntegerField("99.5", commandMetrics.GetExecutionTimePercentile(99.5));
            writer.WriteIntegerField("100", commandMetrics.GetExecutionTimePercentile(100));
            writer.WriteEndObject();

            writer.WriteIntegerField("latencyTotal_mean", commandMetrics.TotalTimeMean);
            writer.WriteObjectFieldStart("latencyTotal");
            writer.WriteIntegerField("0", commandMetrics.GetTotalTimePercentile(0));
            writer.WriteIntegerField("25", commandMetrics.GetTotalTimePercentile(25));
            writer.WriteIntegerField("50", commandMetrics.GetTotalTimePercentile(50));
            writer.WriteIntegerField("75", commandMetrics.GetTotalTimePercentile(75));
            writer.WriteIntegerField("90", commandMetrics.GetTotalTimePercentile(90));
            writer.WriteIntegerField("95", commandMetrics.GetTotalTimePercentile(95));
            writer.WriteIntegerField("99", commandMetrics.GetTotalTimePercentile(99));
            writer.WriteIntegerField("99.5", commandMetrics.GetTotalTimePercentile(99.5));
            writer.WriteIntegerField("100", commandMetrics.GetTotalTimePercentile(100));
            writer.WriteEndObject();

            // property values for reporting what is actually seen by the command rather than what was set somewhere
            var commandProperties = commandMetrics.Properties;

            writer.WriteIntegerField("propertyValue_circuitBreakerRequestVolumeThreshold", commandProperties.CircuitBreakerRequestVolumeThreshold);
            writer.WriteIntegerField("propertyValue_circuitBreakerSleepWindowInMilliseconds", commandProperties.CircuitBreakerSleepWindowInMilliseconds);
            writer.WriteIntegerField("propertyValue_circuitBreakerErrorThresholdPercentage", commandProperties.CircuitBreakerErrorThresholdPercentage);
            writer.WriteBooleanField("propertyValue_circuitBreakerForceOpen", commandProperties.CircuitBreakerForceOpen);
            writer.WriteBooleanField("propertyValue_circuitBreakerForceClosed", commandProperties.CircuitBreakerForceClosed);
            writer.WriteBooleanField("propertyValue_circuitBreakerEnabled", commandProperties.CircuitBreakerEnabled);

            writer.WriteStringField("propertyValue_executionIsolationStrategy", commandProperties.ExecutionIsolationStrategy.ToString());
            writer.WriteIntegerField("propertyValue_executionIsolationThreadTimeoutInMilliseconds", commandProperties.ExecutionTimeoutInMilliseconds);
            writer.WriteBooleanField("propertyValue_executionIsolationThreadInterruptOnTimeout", false);
            writer.WriteStringField("propertyValue_executionIsolationThreadPoolKeyOverride", commandProperties.ExecutionIsolationThreadPoolKeyOverride);
            writer.WriteIntegerField("propertyValue_executionIsolationSemaphoreMaxConcurrentRequests", commandProperties.ExecutionIsolationSemaphoreMaxConcurrentRequests);
            writer.WriteIntegerField("propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests", commandProperties.FallbackIsolationSemaphoreMaxConcurrentRequests);
            writer.WriteIntegerField("propertyValue_metricsRollingStatisticalWindowInMilliseconds", commandProperties.MetricsRollingStatisticalWindowInMilliseconds);
            writer.WriteBooleanField("propertyValue_requestCacheEnabled", commandProperties.RequestCacheEnabled);
            writer.WriteBooleanField("propertyValue_requestLogEnabled", commandProperties.RequestLogEnabled);
            writer.WriteIntegerField("reportingHosts", 1); // this will get summed across all instances in a cluster
            writer.WriteStringField("threadPool", commandMetrics.ThreadPoolKey.Name);
        }
    }
}
