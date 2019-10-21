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

using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial
{
    public static class SerialHystrixDashboardData
    {
        public static string ToJsonString(HystrixDashboardStream.DashboardData data)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    WriteDashboardData(writer, data);
                }

                return sw.ToString();
            }
        }

        public static string ToJsonString(HystrixCommandMetrics commandMetrics)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    WriteCommandMetrics(writer, commandMetrics);
                    return sw.ToString();
                }
            }
        }

        public static string ToJsonString(HystrixThreadPoolMetrics threadPoolMetrics)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    WriteThreadPoolMetrics(writer, threadPoolMetrics);
                    return sw.ToString();
                }
            }
        }

        public static string ToJsonString(HystrixCollapserMetrics collapserMetrics)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    WriteCollapserMetrics(writer, collapserMetrics);
                    return sw.ToString();
                }
            }
        }

        public static List<string> ToMultipleJsonStrings(HystrixDashboardStream.DashboardData dashboardData)
        {
            List<string> jsonStrings = new List<string>();

            foreach (HystrixCommandMetrics commandMetrics in dashboardData.CommandMetrics)
            {
                jsonStrings.Add(ToJsonString(commandMetrics));
            }

            foreach (HystrixThreadPoolMetrics threadPoolMetrics in dashboardData.ThreadPoolMetrics)
            {
                jsonStrings.Add(ToJsonString(threadPoolMetrics));
            }

            foreach (HystrixCollapserMetrics collapserMetrics in dashboardData.CollapserMetrics)
            {
                jsonStrings.Add(ToJsonString(collapserMetrics));
            }

            return jsonStrings;
        }

        private static void WriteDashboardData(JsonTextWriter writer, HystrixDashboardStream.DashboardData data)
        {
            try
            {
                writer.WriteStartArray();
                foreach (HystrixCommandMetrics commandMetrics in data.CommandMetrics)
                {
                    WriteCommandMetrics(writer, commandMetrics);
                }

                foreach (HystrixThreadPoolMetrics threadPoolMetrics in data.ThreadPoolMetrics)
                {
                    WriteThreadPoolMetrics(writer, threadPoolMetrics);
                }

                foreach (HystrixCollapserMetrics collapserMetrics in data.CollapserMetrics)
                {
                    WriteCollapserMetrics(writer, collapserMetrics);
                }

                writer.WriteEndArray();
            }
            catch (Exception)
            {
                // Log
            }
        }

        private static void WriteThreadPoolMetrics(JsonTextWriter writer, HystrixThreadPoolMetrics threadPoolMetrics)
        {
            IHystrixThreadPoolKey key = threadPoolMetrics.ThreadPoolKey;

            writer.WriteStartObject();

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
            writer.WriteLongField("rollingCountThreadsExecuted", threadPoolMetrics.GetRollingCount(ThreadPoolEventType.EXECUTED));

            writer.WriteLongField("rollingMaxActiveThreads", threadPoolMetrics.RollingMaxActiveThreads);
            writer.WriteLongField("rollingCountCommandRejections", threadPoolMetrics.GetRollingCount(ThreadPoolEventType.REJECTED));

            writer.WriteIntegerField("propertyValue_queueSizeRejectionThreshold", threadPoolMetrics.Properties.QueueSizeRejectionThreshold);
            writer.WriteIntegerField("propertyValue_metricsRollingStatisticalWindowInMilliseconds", threadPoolMetrics.Properties.MetricsRollingStatisticalWindowInMilliseconds);

            writer.WriteLongField("reportingHosts", 1); // this will get summed across all instances in a cluster

            writer.WriteEndObject();
        }

        private static void WriteCollapserMetrics(JsonTextWriter writer, HystrixCollapserMetrics collapserMetrics)
        {
            IHystrixCollapserKey key = collapserMetrics.CollapserKey;

            writer.WriteStartObject();

            writer.WriteStringField("type", "HystrixCollapser");
            writer.WriteStringField("name", key.Name);
            writer.WriteLongField("currentTime", Time.CurrentTimeMillisJava);

            writer.WriteLongField("rollingCountRequestsBatched", collapserMetrics.GetRollingCount(CollapserEventType.ADDED_TO_BATCH));

            writer.WriteLongField("rollingCountBatches", collapserMetrics.GetRollingCount(CollapserEventType.BATCH_EXECUTED));

            writer.WriteLongField("rollingCountResponsesFromCache", collapserMetrics.GetRollingCount(CollapserEventType.RESPONSE_FROM_CACHE));

            // batch size percentiles
            writer.WriteIntegerField("batchSize_mean", collapserMetrics.BatchSizeMean);
            writer.WriteObjectFieldStart("batchSize");
            writer.WriteIntegerField("25", collapserMetrics.GetBatchSizePercentile(25));
            writer.WriteIntegerField("50", collapserMetrics.GetBatchSizePercentile(50));
            writer.WriteIntegerField("75", collapserMetrics.GetBatchSizePercentile(75));
            writer.WriteIntegerField("90", collapserMetrics.GetBatchSizePercentile(90));
            writer.WriteIntegerField("95", collapserMetrics.GetBatchSizePercentile(95));
            writer.WriteIntegerField("99", collapserMetrics.GetBatchSizePercentile(99));
            writer.WriteIntegerField("99.5", collapserMetrics.GetBatchSizePercentile(99.5));
            writer.WriteIntegerField("100", collapserMetrics.GetBatchSizePercentile(100));
            writer.WriteEndObject();

            writer.WriteBooleanField("propertyValue_requestCacheEnabled", collapserMetrics.Properties.RequestCacheEnabled);
            writer.WriteIntegerField("propertyValue_maxRequestsInBatch", collapserMetrics.Properties.MaxRequestsInBatch);
            writer.WriteIntegerField("propertyValue_timerDelayInMilliseconds", collapserMetrics.Properties.TimerDelayInMilliseconds);

            writer.WriteIntegerField("reportingHosts", 1); // this will get summed across all instances in a cluster

            writer.WriteEndObject();
        }

        private static void WriteCommandMetrics(JsonTextWriter writer, HystrixCommandMetrics commandMetrics)
        {
            IHystrixCommandKey key = commandMetrics.CommandKey;
            IHystrixCircuitBreaker circuitBreaker = HystrixCircuitBreakerFactory.GetInstance(key);

            writer.WriteStartObject();
            writer.WriteStringField("type", "HystrixCommand");
            writer.WriteStringField("name", key.Name);
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

            HealthCounts healthCounts = commandMetrics.Healthcounts;
            writer.WriteIntegerField("errorPercentage", healthCounts.ErrorPercentage);
            writer.WriteLongField("errorCount", healthCounts.ErrorCount);
            writer.WriteLongField("requestCount", healthCounts.TotalRequests);

            // rolling counters
            writer.WriteLongField("rollingCountBadRequests", commandMetrics.GetRollingCount(HystrixEventType.BAD_REQUEST));
            writer.WriteLongField("rollingCountCollapsedRequests", commandMetrics.GetRollingCount(HystrixEventType.COLLAPSED));
            writer.WriteLongField("rollingCountEmit", commandMetrics.GetRollingCount(HystrixEventType.EMIT));
            writer.WriteLongField("rollingCountExceptionsThrown", commandMetrics.GetRollingCount(HystrixEventType.EXCEPTION_THROWN));
            writer.WriteLongField("rollingCountFailure", commandMetrics.GetRollingCount(HystrixEventType.FAILURE));
            writer.WriteLongField("rollingCountFallbackEmit", commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_EMIT));
            writer.WriteLongField("rollingCountFallbackFailure",  commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_FAILURE));
            writer.WriteLongField("rollingCountFallbackMissing",  commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_MISSING));
            writer.WriteLongField("rollingCountFallbackRejection", commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_REJECTION));
            writer.WriteLongField("rollingCountFallbackSuccess",  commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_SUCCESS));
            writer.WriteLongField("rollingCountResponsesFromCache", commandMetrics.GetRollingCount(HystrixEventType.RESPONSE_FROM_CACHE));
            writer.WriteLongField("rollingCountSemaphoreRejected",  commandMetrics.GetRollingCount(HystrixEventType.SEMAPHORE_REJECTED));
            writer.WriteLongField("rollingCountShortCircuited",  commandMetrics.GetRollingCount(HystrixEventType.SHORT_CIRCUITED));
            writer.WriteLongField("rollingCountSuccess",  commandMetrics.GetRollingCount(HystrixEventType.SUCCESS));
            writer.WriteLongField("rollingCountThreadPoolRejected",  commandMetrics.GetRollingCount(HystrixEventType.THREAD_POOL_REJECTED));
            writer.WriteLongField("rollingCountTimeout", commandMetrics.GetRollingCount(HystrixEventType.TIMEOUT));

            writer.WriteIntegerField("currentConcurrentExecutionCount", commandMetrics.CurrentConcurrentExecutionCount);
            writer.WriteLongField("rollingMaxConcurrentExecutionCount", commandMetrics.RollingMaxConcurrentExecutions);

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
            IHystrixCommandOptions commandProperties = commandMetrics.Properties;

            writer.WriteIntegerField("propertyValue_circuitBreakerRequestVolumeThreshold", commandProperties.CircuitBreakerRequestVolumeThreshold);
            writer.WriteIntegerField("propertyValue_circuitBreakerSleepWindowInMilliseconds", commandProperties.CircuitBreakerSleepWindowInMilliseconds);
            writer.WriteIntegerField("propertyValue_circuitBreakerErrorThresholdPercentage", commandProperties.CircuitBreakerErrorThresholdPercentage);
            writer.WriteBooleanField("propertyValue_circuitBreakerForceOpen", commandProperties.CircuitBreakerForceOpen);
            writer.WriteBooleanField("propertyValue_circuitBreakerForceClosed", commandProperties.CircuitBreakerForceClosed);
            writer.WriteBooleanField("propertyValue_circuitBreakerEnabled", commandProperties.CircuitBreakerEnabled);

            writer.WriteStringField("propertyValue_executionIsolationStrategy", commandProperties.ExecutionIsolationStrategy.ToString());
            writer.WriteIntegerField("propertyValue_executionIsolationThreadTimeoutInMilliseconds", commandProperties.ExecutionTimeoutInMilliseconds);
            writer.WriteIntegerField("propertyValue_executionTimeoutInMilliseconds", commandProperties.ExecutionTimeoutInMilliseconds);
            writer.WriteBooleanField("propertyValue_executionIsolationThreadInterruptOnTimeout", false);
            writer.WriteStringField("propertyValue_executionIsolationThreadPoolKeyOverride", commandProperties.ExecutionIsolationThreadPoolKeyOverride);
            writer.WriteIntegerField("propertyValue_executionIsolationSemaphoreMaxConcurrentRequests", commandProperties.ExecutionIsolationSemaphoreMaxConcurrentRequests);
            writer.WriteIntegerField("propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests", commandProperties.FallbackIsolationSemaphoreMaxConcurrentRequests);
            writer.WriteIntegerField("propertyValue_metricsRollingStatisticalWindowInMilliseconds", commandProperties.MetricsRollingStatisticalWindowInMilliseconds);
            writer.WriteBooleanField("propertyValue_requestCacheEnabled", commandProperties.RequestCacheEnabled);
            writer.WriteBooleanField("propertyValue_requestLogEnabled", commandProperties.RequestLogEnabled);
            writer.WriteIntegerField("reportingHosts", 1); // this will get summed across all instances in a cluster
            writer.WriteStringField("threadPool", commandMetrics.ThreadPoolKey.Name);
            writer.WriteEndObject();
        }
    }
}
