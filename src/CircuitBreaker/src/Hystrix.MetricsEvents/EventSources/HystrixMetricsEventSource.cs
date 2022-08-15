// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.EventSources;

[EventSource(Name = "Steeltoe.Hystrix.Events")]
public class HystrixMetricsEventSource : EventSource
{
    private static readonly Lazy<HystrixMetricsEventSource> Instance = new(() => new HystrixMetricsEventSource());

    public static HystrixMetricsEventSource EventLogger => Instance.Value;

    private HystrixMetricsEventSource()
    {
    }

    [Event(1, Message = "CommandMetrics", Level = EventLevel.Verbose, Keywords = Keywords.Command)]
    public void CommandMetrics(string commandKey, string commandGroup, bool isCircuitBreakerOpen, long errorCount, long requestCount,
        int currentConcurrentExecutionCount, int latencyExecuteMean, int latencyTotalMean, int reportingHosts, string threadPool)
    {
        if (IsEnabled())
        {
            WriteEvent(1, commandKey, commandGroup, isCircuitBreakerOpen, errorCount, requestCount, currentConcurrentExecutionCount, latencyExecuteMean,
                latencyTotalMean, reportingHosts, threadPool);
        }
    }

    [Event(2, Message = "ThreadPoolMetrics", Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool)]
    internal void ThreadPoolMetrics(string threadpoolKey, long cumulativeCountThreadsExecuted, int currentActiveCount, int currentCompletedTaskCount,
        int currentCorePoolSize, int currentLargestPoolSize, int currentMaximumPoolSize, int currentPoolSize, int currentQueueSize, int currentTaskCount,
        int reportingHosts)
    {
        if (IsEnabled())
        {
            WriteEvent(2, threadpoolKey, cumulativeCountThreadsExecuted, currentActiveCount, currentCompletedTaskCount, currentCorePoolSize,
                currentLargestPoolSize, currentMaximumPoolSize, currentPoolSize, currentQueueSize, currentTaskCount, reportingHosts);
        }
    }

    [Event(3, Message = "CollapserMetrics", Level = EventLevel.Verbose, Keywords = Keywords.Collapser)]
    internal void CollapserMetrics(string collapserKey, long rollingCountRequestsBatched, long rollingCountBatches, long rollingCountResponsesFromCache,
        int batchSizeMean, int reportingHosts)
    {
        if (IsEnabled())
        {
            WriteEvent(3, collapserKey, rollingCountRequestsBatched, rollingCountBatches, rollingCountResponsesFromCache, batchSizeMean, reportingHosts);
        }
    }

    public class Keywords
    {
        public const EventKeywords Command = (EventKeywords)1;
        public const EventKeywords ThreadPool = (EventKeywords)2;
        public const EventKeywords Collapser = (EventKeywords)4;

        protected Keywords()
        {
        }
    }
}
