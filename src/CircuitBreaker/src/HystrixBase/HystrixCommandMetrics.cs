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
        private static readonly IList<HystrixEventType> ALL_EVENT_TYPES = HystrixEventTypeHelper.Values;

        public static Func<long[], HystrixCommandCompletion, long[]> AppendEventToBucket { get; } = (initialCountArray, execution) =>
        {
            ExecutionResult.EventCounts eventCounts = execution.Eventcounts;
            foreach (HystrixEventType eventType in ALL_EVENT_TYPES)
            {
                switch (eventType)
                {
                    case HystrixEventType.EXCEPTION_THROWN: break; // this is just a sum of other anyway - don't do the work here
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
                        foreach (HystrixEventType exceptionEventType in HystrixEventTypeHelper.ExceptionProducingEventTypes)
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

        private static readonly ConcurrentDictionary<string, HystrixCommandMetrics> Metrics = new ConcurrentDictionary<string, HystrixCommandMetrics>();

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

            return Metrics.GetOrAddEx(key.Name, (k) => new HystrixCommandMetrics(key, commandGroup, nonNullThreadPoolKey, properties, HystrixPlugins.EventNotifier));
        }

        public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key)
        {
            Metrics.TryGetValue(key.Name, out HystrixCommandMetrics result);
            return result;
        }

        public static ICollection<HystrixCommandMetrics> GetInstances()
        {
            List<HystrixCommandMetrics> commandMetrics = new List<HystrixCommandMetrics>();
            foreach (HystrixCommandMetrics tpm in Metrics.Values)
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

            RollingCommandEventCounterStream.Reset();
            CumulativeCommandEventCounterStream.Reset();
            RollingCommandLatencyDistributionStream.Reset();
            RollingCommandUserLatencyDistributionStream.Reset();
            RollingCommandMaxConcurrencyStream.Reset();
            HystrixThreadEventStream.Reset();
            HealthCountsStream.Reset();

            Metrics.Clear();
        }

        private readonly AtomicInteger concurrentExecutionCount = new AtomicInteger();

        private readonly RollingCommandEventCounterStream rollingCommandEventCounterStream;
        private readonly CumulativeCommandEventCounterStream cumulativeCommandEventCounterStream;
        private readonly RollingCommandLatencyDistributionStream rollingCommandLatencyDistributionStream;
        private readonly RollingCommandUserLatencyDistributionStream rollingCommandUserLatencyDistributionStream;
        private readonly RollingCommandMaxConcurrencyStream rollingCommandMaxConcurrencyStream;

        private readonly object _syncLock = new object();

        private HealthCountsStream healthCountsStream;

        internal HystrixCommandMetrics(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixThreadPoolKey threadPoolKey, IHystrixCommandOptions properties, HystrixEventNotifier eventNotifier)
            : base(null)
        {
            CommandKey = key;
            CommandGroup = commandGroup;
            ThreadPoolKey = threadPoolKey;
            Properties = properties;

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
                HealthCountsStream.RemoveByKey(CommandKey);
                healthCountsStream = HealthCountsStream.GetInstance(CommandKey, Properties);
            }
        }

        public IHystrixCommandKey CommandKey { get; }

        public IHystrixCommandGroupKey CommandGroup { get; }

        public IHystrixThreadPoolKey ThreadPoolKey { get; }

        public IHystrixCommandOptions Properties { get; }

        public long GetRollingCount(HystrixEventType eventType)
        {
            return rollingCommandEventCounterStream.GetLatest(eventType);
        }

        public override long GetRollingCount(HystrixRollingNumberEvent @event)
        {
            return GetRollingCount(HystrixEventTypeHelper.From(@event));
        }

        public long GetCumulativeCount(HystrixEventType eventType)
        {
            return cumulativeCommandEventCounterStream.GetLatest(eventType);
        }

        public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
        {
            return GetCumulativeCount(HystrixEventTypeHelper.From(@event));
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

        public int TotalTimeMean => rollingCommandUserLatencyDistributionStream.LatestMean;

        public long RollingMaxConcurrentExecutions => rollingCommandMaxConcurrencyStream.LatestRollingMax;

        public int CurrentConcurrentExecutionCount => concurrentExecutionCount.Value;

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

        public HealthCounts Healthcounts => healthCountsStream.Latest;

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
}