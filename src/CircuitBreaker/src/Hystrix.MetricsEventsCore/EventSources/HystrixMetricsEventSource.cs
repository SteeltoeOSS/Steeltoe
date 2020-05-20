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

using System;
using System.Diagnostics.Tracing;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources
{
    [EventSource(Name ="Steeltoe.Hystrix.Events")]
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
            = new Lazy<HystrixMetricsEventSource>(() => new HystrixMetricsEventSource());

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
