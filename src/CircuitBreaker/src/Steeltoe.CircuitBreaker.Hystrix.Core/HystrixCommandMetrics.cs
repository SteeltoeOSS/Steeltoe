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

using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.EventNotifier;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixCommandMetrics : HystrixMetrics
    {
        //private static final Logger logger = LoggerFactory.getLogger(HystrixCommandMetrics.class);

        private static readonly IList<HystrixEventType> ALL_EVENT_TYPES = HystrixEventTypeHelper.Values;

        public static Func<long[], HystrixCommandCompletion, long[]> AppendEventToBucket { get; } = (initialCountArray, execution) =>
        {
        
            ExecutionResult.EventCounts eventCounts = execution.Eventcounts;
            foreach (HystrixEventType eventType in ALL_EVENT_TYPES)
            {
                switch (eventType)
                {
                    case HystrixEventType.EXCEPTION_THROWN: break; //this is just a sum of other anyway - don't do the work here
                    default:
                        var ordinal = (int)eventType;
                        initialCountArray[ordinal] += eventCounts.GetCount(eventType);
                        break;
                }
            }
            return initialCountArray;
            
        };

        public static Func<long[], long[], long[]> BucketAggregator { get; } = (cumulativeEvents, bucketEventCounts) =>
        {
   
            foreach (HystrixEventType eventType in ALL_EVENT_TYPES)
            {
                switch (eventType)
                {
                    case HystrixEventType.EXCEPTION_THROWN:
                        foreach (HystrixEventType exceptionEventType in HystrixEventTypeHelper.EXCEPTION_PRODUCING_EVENT_TYPES)
                        {
                            var ordinal1 = (int)eventType;
                            var ordinal2 = (int)exceptionEventType;
                            cumulativeEvents[ordinal1] += bucketEventCounts[ordinal2];
                        }
                        break;
                    default:
                        var ordinal = (int)eventType;
                        cumulativeEvents[ordinal] += bucketEventCounts[ordinal];
                        break;
                }
            }
            return cumulativeEvents;
            
        };

        private static readonly ConcurrentDictionary<string, HystrixCommandMetrics> metrics = new ConcurrentDictionary<string, HystrixCommandMetrics>();


        public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixCommandOptions properties)
        {
            return GetInstance(key, commandGroup, null, properties);
        }

        public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixThreadPoolKey threadPoolKey, IHystrixCommandOptions properties)
        {
            // attempt to retrieve from cache first
            IHystrixThreadPoolKey nonNullThreadPoolKey;
            if (threadPoolKey == null)
            {
                nonNullThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey(commandGroup.Name);
            }
            else
            {
                nonNullThreadPoolKey = threadPoolKey;
            }

            return metrics.GetOrAddEx(key.Name, (k) => new HystrixCommandMetrics(key, commandGroup, nonNullThreadPoolKey, properties, HystrixPlugins.EventNotifier));
        }

        public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key)
        {
            HystrixCommandMetrics result = null;
            metrics.TryGetValue(key.Name, out result);
            return result;
        }

        public static ICollection<HystrixCommandMetrics> GetInstances()
        {
            List<HystrixCommandMetrics> commandMetrics = new List<HystrixCommandMetrics>();
            foreach (HystrixCommandMetrics tpm in metrics.Values)
            {
                commandMetrics.Add(tpm);
            }
            return commandMetrics.AsReadOnly();
        }



        internal static void Reset()
        {
            foreach (HystrixCommandMetrics metricsInstance in GetInstances())
            {
                metricsInstance.UnsubscribeAll();
            }
            metrics.Clear();
        }

        private readonly IHystrixCommandOptions properties;
        private readonly IHystrixCommandKey key;
        private readonly IHystrixCommandGroupKey group;
        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly AtomicInteger concurrentExecutionCount = new AtomicInteger();

        private HealthCountsStream healthCountsStream;
        private readonly RollingCommandEventCounterStream rollingCommandEventCounterStream;
        private readonly CumulativeCommandEventCounterStream cumulativeCommandEventCounterStream;
        private readonly RollingCommandLatencyDistributionStream rollingCommandLatencyDistributionStream;
        private readonly RollingCommandUserLatencyDistributionStream rollingCommandUserLatencyDistributionStream;
        private readonly RollingCommandMaxConcurrencyStream rollingCommandMaxConcurrencyStream;

        private readonly Object _syncLock = new object();
        HystrixCommandMetrics(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixThreadPoolKey threadPoolKey, IHystrixCommandOptions properties, HystrixEventNotifier eventNotifier) : base(null)
        {
            this.key = key;
            this.group = commandGroup;
            this.threadPoolKey = threadPoolKey;
            this.properties = properties;

            healthCountsStream = HealthCountsStream.GetInstance(key, properties);
            rollingCommandEventCounterStream = RollingCommandEventCounterStream.GetInstance(key, properties);
            cumulativeCommandEventCounterStream = CumulativeCommandEventCounterStream.GetInstance(key, properties);

            rollingCommandLatencyDistributionStream = RollingCommandLatencyDistributionStream.GetInstance(key, properties);
            rollingCommandUserLatencyDistributionStream = RollingCommandUserLatencyDistributionStream.GetInstance(key, properties);
            rollingCommandMaxConcurrencyStream = RollingCommandMaxConcurrencyStream.GetInstance(key, properties);
        }

        /* package */
        internal void ResetStream()
        {
            lock (_syncLock)
            {
                healthCountsStream.Unsubscribe();
                HealthCountsStream.RemoveByKey(key);
                healthCountsStream = HealthCountsStream.GetInstance(key, properties);
            }
        }

        public IHystrixCommandKey CommandKey
        {
            get { return key; }
        }


        public IHystrixCommandGroupKey CommandGroup
        {
            get { return group; }
        }

        public IHystrixThreadPoolKey ThreadPoolKey
        {
            get { return threadPoolKey; }
        }


        public IHystrixCommandOptions Properties
        {
            get { return properties; }
        }

        public long GetRollingCount(HystrixEventType eventType)
        {
            return rollingCommandEventCounterStream.GetLatest(eventType);
        }

        public long GetCumulativeCount(HystrixEventType eventType)
        {
            return cumulativeCommandEventCounterStream.GetLatest(eventType);
        }

        public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
        {
            return GetCumulativeCount(HystrixEventTypeHelper.From(@event));
        }

        public override long GetRollingCount(HystrixRollingNumberEvent @event)
        {
            return GetRollingCount(HystrixEventTypeHelper.From(@event));
        }


        public int GetExecutionTimePercentile(double percentile)
        {
            return rollingCommandLatencyDistributionStream.GetLatestPercentile(percentile);
        }


        public int ExecutionTimeMean
        {
            get { return rollingCommandLatencyDistributionStream.LatestMean; }
        }


        public int GetTotalTimePercentile(double percentile)
        {
            return rollingCommandUserLatencyDistributionStream.GetLatestPercentile(percentile);
        }


        public int TotalTimeMean
        {
            get { return rollingCommandUserLatencyDistributionStream.LatestMean; }
        }

        public long RollingMaxConcurrentExecutions
        {
            get { return rollingCommandMaxConcurrencyStream.LatestRollingMax; }
        }


        public int CurrentConcurrentExecutionCount
        {
            get { return concurrentExecutionCount.Value; }
        }
      
        internal void MarkCommandStart(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy)
        {
            int currentCount = concurrentExecutionCount.IncrementAndGet();
            HystrixThreadEventStream.GetInstance().CommandExecutionStarted(commandKey, threadPoolKey, isolationStrategy, currentCount);
        }
        internal void MarkCommandDone(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, bool executionStarted)
        {
            HystrixThreadEventStream.GetInstance().ExecutionDone(executionResult, commandKey, threadPoolKey);
            if (executionStarted)
            {
                concurrentExecutionCount.DecrementAndGet();
            }
        }


        public HealthCounts Healthcounts
        {
            get { return healthCountsStream.Latest; }
        }

        private void UnsubscribeAll()
        {
            healthCountsStream.Unsubscribe();
            rollingCommandEventCounterStream.Unsubscribe();
            cumulativeCommandEventCounterStream.Unsubscribe();
            rollingCommandLatencyDistributionStream.Unsubscribe();
            rollingCommandUserLatencyDistributionStream.Unsubscribe();
            rollingCommandMaxConcurrencyStream.Unsubscribe();
        }


    }


    public class HealthCounts
    {
        private readonly long totalCount;
        private readonly long errorCount;
        private readonly int errorPercentage;

        HealthCounts(long total, long error)
        {
            this.totalCount = total;
            this.errorCount = error;
            if (totalCount > 0)
            {
                this.errorPercentage = (int)((errorCount * 100) / totalCount);
            }
            else
            {
                this.errorPercentage = 0;
            }
        }

        private static readonly HealthCounts EMPTY = new HealthCounts(0, 0);

        public long TotalRequests
        {
            get { return totalCount; }
        }

        public long ErrorCount
        {
            get { return errorCount; }
        }

        public int ErrorPercentage
        {
            get { return errorPercentage; }
        }

        public HealthCounts Plus(long[] eventTypeCounts)
        {
            long updatedTotalCount = totalCount;
            long updatedErrorCount = errorCount;

            long successCount = eventTypeCounts[(int)HystrixEventType.SUCCESS];
            long failureCount = eventTypeCounts[(int)HystrixEventType.FAILURE];
            long timeoutCount = eventTypeCounts[(int)HystrixEventType.TIMEOUT];
            long threadPoolRejectedCount = eventTypeCounts[(int)HystrixEventType.THREAD_POOL_REJECTED];
            long semaphoreRejectedCount = eventTypeCounts[(int)HystrixEventType.SEMAPHORE_REJECTED];

            updatedTotalCount += (successCount + failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount);
            updatedErrorCount += (failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount);
            return new HealthCounts(updatedTotalCount, updatedErrorCount);
        }

        public static HealthCounts Empty
        {
            get { return EMPTY; }
        }

        public override string ToString()
        {
            return "HealthCounts[" + errorCount + " / " + totalCount + " : " + ErrorPercentage + "%]";
        }
    }
}