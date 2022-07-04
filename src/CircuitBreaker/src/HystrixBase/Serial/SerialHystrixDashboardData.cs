// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial;

public static class SerialHystrixDashboardData
{
    public static string ToJsonString(HystrixDashboardStream.DashboardData data)
    {
        using var sw = new StringWriter();
        using (var writer = new JsonTextWriter(sw))
        {
            WriteDashboardData(writer, data);
        }

        return sw.ToString();
    }

    public static string ToJsonString(HystrixCommandMetrics commandMetrics)
    {
        using var sw = new StringWriter();
        using var writer = new JsonTextWriter(sw);
        try
        {
            WriteCommandMetrics(writer, commandMetrics);
        }
        catch (Exception)
        {
            // failing to write metrics should not crash the app
        }

        return sw.ToString();
    }

    public static string ToJsonString(HystrixThreadPoolMetrics threadPoolMetrics)
    {
        using var sw = new StringWriter();
        using var writer = new JsonTextWriter(sw);
        try
        {
            WriteThreadPoolMetrics(writer, threadPoolMetrics);
        }
        catch (Exception)
        {
            // failing to write metrics should not crash the app
        }

        return sw.ToString();
    }

    public static string ToJsonString(HystrixCollapserMetrics collapserMetrics)
    {
        using var sw = new StringWriter();
        using var writer = new JsonTextWriter(sw);
        try
        {
            WriteCollapserMetrics(writer, collapserMetrics);
        }
        catch (Exception)
        {
            // failing to write metrics should not crash the app
        }

        return sw.ToString();
    }

    public static List<string> ToMultipleJsonStrings(HystrixDashboardStream.DashboardData dashboardData)
    {
        var jsonStrings = new List<string>();

        foreach (var commandMetrics in dashboardData.CommandMetrics)
        {
            jsonStrings.Add(ToJsonString(commandMetrics));
        }

        foreach (var threadPoolMetrics in dashboardData.ThreadPoolMetrics)
        {
            jsonStrings.Add(ToJsonString(threadPoolMetrics));
        }

        foreach (var collapserMetrics in dashboardData.CollapserMetrics)
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
            foreach (var commandMetrics in data.CommandMetrics)
            {
                WriteCommandMetrics(writer, commandMetrics);
            }

            foreach (var threadPoolMetrics in data.ThreadPoolMetrics)
            {
                WriteThreadPoolMetrics(writer, threadPoolMetrics);
            }

            foreach (var collapserMetrics in data.CollapserMetrics)
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
        var key = threadPoolMetrics.ThreadPoolKey;

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
        writer.WriteLongField("rollingCountThreadsExecuted", threadPoolMetrics.GetRollingCount(ThreadPoolEventType.Executed));

        writer.WriteLongField("rollingMaxActiveThreads", threadPoolMetrics.RollingMaxActiveThreads);
        writer.WriteLongField("rollingCountCommandRejections", threadPoolMetrics.GetRollingCount(ThreadPoolEventType.Rejected));

        writer.WriteIntegerField("propertyValue_queueSizeRejectionThreshold", threadPoolMetrics.Properties.QueueSizeRejectionThreshold);
        writer.WriteIntegerField("propertyValue_metricsRollingStatisticalWindowInMilliseconds", threadPoolMetrics.Properties.MetricsRollingStatisticalWindowInMilliseconds);

        writer.WriteLongField("reportingHosts", 1); // this will get summed across all instances in a cluster

        writer.WriteEndObject();
    }

    private static void WriteCollapserMetrics(JsonTextWriter writer, HystrixCollapserMetrics collapserMetrics)
    {
        var key = collapserMetrics.CollapserKey;

        writer.WriteStartObject();

        writer.WriteStringField("type", "HystrixCollapser");
        writer.WriteStringField("name", key.Name);
        writer.WriteLongField("currentTime", Time.CurrentTimeMillisJava);

        writer.WriteLongField("rollingCountRequestsBatched", collapserMetrics.GetRollingCount(CollapserEventType.AddedToBatch));

        writer.WriteLongField("rollingCountBatches", collapserMetrics.GetRollingCount(CollapserEventType.BatchExecuted));

        writer.WriteLongField("rollingCountResponsesFromCache", collapserMetrics.GetRollingCount(CollapserEventType.ResponseFromCache));

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
        var key = commandMetrics.CommandKey;
        var circuitBreaker = HystrixCircuitBreakerFactory.GetInstance(key);

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

        var healthCounts = commandMetrics.Healthcounts;
        writer.WriteIntegerField("errorPercentage", healthCounts.ErrorPercentage);
        writer.WriteLongField("errorCount", healthCounts.ErrorCount);
        writer.WriteLongField("requestCount", healthCounts.TotalRequests);

        // rolling counters
        writer.WriteLongField("rollingCountBadRequests", commandMetrics.GetRollingCount(HystrixEventType.BadRequest));
        writer.WriteLongField("rollingCountCollapsedRequests", commandMetrics.GetRollingCount(HystrixEventType.Collapsed));
        writer.WriteLongField("rollingCountEmit", commandMetrics.GetRollingCount(HystrixEventType.Emit));
        writer.WriteLongField("rollingCountExceptionsThrown", commandMetrics.GetRollingCount(HystrixEventType.ExceptionThrown));
        writer.WriteLongField("rollingCountFailure", commandMetrics.GetRollingCount(HystrixEventType.Failure));
        writer.WriteLongField("rollingCountFallbackEmit", commandMetrics.GetRollingCount(HystrixEventType.FallbackEmit));
        writer.WriteLongField("rollingCountFallbackFailure", commandMetrics.GetRollingCount(HystrixEventType.FallbackFailure));
        writer.WriteLongField("rollingCountFallbackMissing", commandMetrics.GetRollingCount(HystrixEventType.FallbackMissing));
        writer.WriteLongField("rollingCountFallbackRejection", commandMetrics.GetRollingCount(HystrixEventType.FallbackRejection));
        writer.WriteLongField("rollingCountFallbackSuccess", commandMetrics.GetRollingCount(HystrixEventType.FallbackSuccess));
        writer.WriteLongField("rollingCountResponsesFromCache", commandMetrics.GetRollingCount(HystrixEventType.ResponseFromCache));
        writer.WriteLongField("rollingCountSemaphoreRejected", commandMetrics.GetRollingCount(HystrixEventType.SemaphoreRejected));
        writer.WriteLongField("rollingCountShortCircuited", commandMetrics.GetRollingCount(HystrixEventType.ShortCircuited));
        writer.WriteLongField("rollingCountSuccess", commandMetrics.GetRollingCount(HystrixEventType.Success));
        writer.WriteLongField("rollingCountThreadPoolRejected", commandMetrics.GetRollingCount(HystrixEventType.ThreadPoolRejected));
        writer.WriteLongField("rollingCountTimeout", commandMetrics.GetRollingCount(HystrixEventType.Timeout));

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
        var commandProperties = commandMetrics.Properties;

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
