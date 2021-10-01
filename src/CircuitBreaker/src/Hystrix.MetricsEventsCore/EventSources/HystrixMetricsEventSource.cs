// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources
{
    [EventSource(Name = "Steeltoe.Hystrix.Events")]
    public class HystrixMetricsEventSource : EventSource
    {
        public class Keywords
        {
            public const EventKeywords Command = (EventKeywords)1;
            public const EventKeywords ThreadPool = (EventKeywords)2;
            public const EventKeywords Collapser = (EventKeywords)4;

            protected Keywords()
            {
            }
        }

        private static readonly Lazy<HystrixMetricsEventSource> Instance
            = new (() => new HystrixMetricsEventSource());

        private HystrixMetricsEventSource()
        {
        }

        public static HystrixMetricsEventSource EventLogger
        {
            get { return Instance.Value; }
        }

        [Event(1, Message = "CommandMetrics", Level = EventLevel.Verbose, Keywords = Keywords.Command)]
        public void CommandMetrics(
            string commandKey,
            string commandGroup,
            bool isCiruitBreakerOpen,
            long errorCount,
            long requestCount,
            int currentConcurrentExecutionCount,
            int latencyExecute_mean,
            int latencyTotal_mean,
            int reportingHosts,
            string threadPool)
        {
            if (IsEnabled())
            {
                WriteEvent(
                  1,
                  commandKey,
                  commandGroup,
                  isCiruitBreakerOpen,
                  errorCount,
                  requestCount,
                  currentConcurrentExecutionCount,
                  latencyExecute_mean,
                  latencyTotal_mean,
                  reportingHosts,
                  threadPool);
            }
        }

        [Event(2, Message = "ThreadPoolMetrics", Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool)]
        internal void ThreadPoolMetrics(
            string threadpoolKey,
            long cumulativeCountThreadsExecuted,
            int currentActiveCount,
            int currentCompletedTaskCount,
            int currentCorePoolSize,
            int currentLargestPoolSize,
            int currentMaximumPoolSize,
            int currentPoolSize,
            int currentQueueSize,
            int currentTaskCount,
            int reportingHosts)
        {
            if (IsEnabled())
            {
                WriteEvent(
                    2,
                    threadpoolKey,
                    cumulativeCountThreadsExecuted,
                    currentActiveCount,
                    currentCompletedTaskCount,
                    currentCorePoolSize,
                    currentLargestPoolSize,
                    currentMaximumPoolSize,
                    currentPoolSize,
                    currentQueueSize,
                    currentTaskCount,
                    reportingHosts);
            }
        }

        [Event(3, Message = "CollapserMetrics", Level = EventLevel.Verbose, Keywords = Keywords.Collapser)]
        internal void CollapserMetrics(
            string collapserKey,
            long rollingCountRequestsBatched,
            long rollingCountBatches,
            long rollingCountResponsesFromCache,
            int batchSize_mean,
            int reportingHosts)
        {
            if (IsEnabled())
            {
                WriteEvent(
                    3,
                    collapserKey,
                    rollingCountRequestsBatched,
                    rollingCountBatches,
                    rollingCountResponsesFromCache,
                    batchSize_mean,
                    reportingHosts);
            }
        }
    }
}
