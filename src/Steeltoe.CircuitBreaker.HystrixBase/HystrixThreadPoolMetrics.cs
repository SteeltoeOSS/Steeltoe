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
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixThreadPoolMetrics : HystrixMetrics
    {
        private static readonly IList<HystrixEventType> ALL_COMMAND_EVENT_TYPES = HystrixEventTypeHelper.Values;
        private static readonly IList<ThreadPoolEventType> ALL_THREADPOOL_EVENT_TYPES = ThreadPoolEventTypeHelper.Values;
        private static readonly int NUMBER_THREADPOOL_EVENT_TYPES = ALL_THREADPOOL_EVENT_TYPES.Count;

        // String is HystrixThreadPoolKey.name() (we can't use HystrixThreadPoolKey directly as we can't guarantee it implements hashcode/equals correctly)
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolMetrics> Metrics = new ConcurrentDictionary<string, HystrixThreadPoolMetrics>();

        public static HystrixThreadPoolMetrics GetInstance(IHystrixThreadPoolKey key, IHystrixTaskScheduler taskScheduler, IHystrixThreadPoolOptions properties)
        {
            return Metrics.GetOrAddEx(key.Name, (k) => new HystrixThreadPoolMetrics(key, taskScheduler, properties));
        }

        public static HystrixThreadPoolMetrics GetInstance(IHystrixThreadPoolKey key)
        {
            Metrics.TryGetValue(key.Name, out HystrixThreadPoolMetrics result);
            return result;
        }

        public static ICollection<HystrixThreadPoolMetrics> GetInstances()
        {
            List<HystrixThreadPoolMetrics> threadPoolMetrics = new List<HystrixThreadPoolMetrics>();
            foreach (HystrixThreadPoolMetrics tpm in Metrics.Values)
            {
                if (HasExecutedCommandsOnThread(tpm))
                {
                    threadPoolMetrics.Add(tpm);
                }
            }

            return threadPoolMetrics.AsReadOnly();
        }

        private static bool HasExecutedCommandsOnThread(HystrixThreadPoolMetrics threadPoolMetrics)
        {
            return threadPoolMetrics.CurrentCompletedTaskCount > 0;
        }

        public static Func<long[], HystrixCommandCompletion, long[]> AppendEventToBucket { get; } = (initialCountArray, execution) =>
        {
            ExecutionResult.EventCounts eventCounts = execution.Eventcounts;
            foreach (HystrixEventType eventType in ALL_COMMAND_EVENT_TYPES)
            {
                long eventCount = eventCounts.GetCount(eventType);
                ThreadPoolEventType threadPoolEventType = ThreadPoolEventTypeHelper.From(eventType);
                if (threadPoolEventType != ThreadPoolEventType.UNKNOWN)
                {
                    long ordinal = (long)threadPoolEventType;
                    initialCountArray[ordinal] += eventCount;
                }
            }

            return initialCountArray;
        };

        public static Func<long[], long[], long[]> CounterAggregator { get; } = (cumulativeEvents, bucketEventCounts) =>
        {
            for (int i = 0; i < NUMBER_THREADPOOL_EVENT_TYPES; i++)
            {
                cumulativeEvents[i] += bucketEventCounts[i];
            }

            return cumulativeEvents;
        };

        internal static void Reset()
        {
            Metrics.Clear();
        }

        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly IHystrixTaskScheduler threadPool;
        private readonly IHystrixThreadPoolOptions properties;

        private readonly AtomicInteger concurrentExecutionCount = new AtomicInteger();

        private readonly RollingThreadPoolEventCounterStream rollingCounterStream;
        private readonly CumulativeThreadPoolEventCounterStream cumulativeCounterStream;
        private readonly RollingThreadPoolMaxConcurrencyStream rollingThreadPoolMaxConcurrencyStream;

        private HystrixThreadPoolMetrics(IHystrixThreadPoolKey threadPoolKey, IHystrixTaskScheduler threadPool, IHystrixThreadPoolOptions properties)
            : base(null)
        {
            this.threadPoolKey = threadPoolKey;
            this.threadPool = threadPool;
            this.properties = properties;

            rollingCounterStream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, properties);
            cumulativeCounterStream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, properties);
            rollingThreadPoolMaxConcurrencyStream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, properties);
        }

        public static Func<int> GetCurrentConcurrencyThunk(IHystrixThreadPoolKey threadPoolKey)
        {
            return () =>
            {
                return HystrixThreadPoolMetrics.GetInstance(threadPoolKey).concurrentExecutionCount.Value;
            };
        }

        public IHystrixTaskScheduler TaskScheduler
        {
            get { return threadPool; }
        }

        public IHystrixThreadPoolKey ThreadPoolKey
        {
            get { return threadPoolKey; }
        }

        public IHystrixThreadPoolOptions Properties
        {
            get { return properties; }
        }

        public int CurrentActiveCount
        {
            get { return threadPool.CurrentActiveCount; }
        }

        public int CurrentCompletedTaskCount
        {
            get { return threadPool.CurrentCompletedTaskCount; }
        }

        public int CurrentCorePoolSize
        {
            get { return threadPool.CurrentCorePoolSize; }
        }

        public int CurrentLargestPoolSize
        {
            get { return threadPool.CurrentLargestPoolSize; }
        }

        public int CurrentMaximumPoolSize
        {
            get { return threadPool.CurrentMaximumPoolSize; }
        }

        public int CurrentPoolSize
        {
            get { return threadPool.CurrentPoolSize; }
        }

        public int CurrentTaskCount
        {
            get { return threadPool.CurrentTaskCount; }
        }

        public int CurrentQueueSize
        {
            get { return threadPool.CurrentQueueSize; }
        }

        public void MarkThreadExecution()
        {
            concurrentExecutionCount.IncrementAndGet();
        }

        public long RollingCountThreadsExecuted
        {
            get { return rollingCounterStream.GetLatestCount(ThreadPoolEventType.EXECUTED); }
        }

        public long CumulativeCountThreadsExecuted
        {
            get { return cumulativeCounterStream.GetLatestCount(ThreadPoolEventType.EXECUTED); }
        }

        public long RollingCountThreadsRejected
        {
            get { return rollingCounterStream.GetLatestCount(ThreadPoolEventType.REJECTED); }
        }

        public long CumulativeCountThreadsRejected
        {
            get { return cumulativeCounterStream.GetLatestCount(ThreadPoolEventType.REJECTED); }
        }

        public long GetRollingCount(ThreadPoolEventType @event)
        {
            return rollingCounterStream.GetLatestCount(@event);
        }

        public long GetCumulativeCount(ThreadPoolEventType @event)
        {
            return cumulativeCounterStream.GetLatestCount(@event);
        }

        public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
        {
            return cumulativeCounterStream.GetLatestCount(ThreadPoolEventTypeHelper.From(@event));
        }

        public override long GetRollingCount(HystrixRollingNumberEvent @event)
        {
            return rollingCounterStream.GetLatestCount(ThreadPoolEventTypeHelper.From(@event));
        }

        public void MarkThreadCompletion()
        {
            concurrentExecutionCount.DecrementAndGet();
        }

        public long RollingMaxActiveThreads
        {
            get { return rollingThreadPoolMaxConcurrencyStream.LatestRollingMax; }
        }

        public void MarkThreadRejection()
        {
            concurrentExecutionCount.DecrementAndGet();
        }
    }
}
