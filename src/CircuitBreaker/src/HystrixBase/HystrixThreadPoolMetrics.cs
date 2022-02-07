// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common;
using Steeltoe.Common.Util;
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
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolMetrics> Metrics = new ();

        public static HystrixThreadPoolMetrics GetInstance(IHystrixThreadPoolKey key, IHystrixTaskScheduler taskScheduler, IHystrixThreadPoolOptions properties)
        {
            return Metrics.GetOrAddEx(key.Name, (k) => new HystrixThreadPoolMetrics(key, taskScheduler, properties));
        }

        public static HystrixThreadPoolMetrics GetInstance(IHystrixThreadPoolKey key)
        {
            Metrics.TryGetValue(key.Name, out var result);
            return result;
        }

        public static ICollection<HystrixThreadPoolMetrics> GetInstances()
        {
            var threadPoolMetrics = new List<HystrixThreadPoolMetrics>();
            foreach (var tpm in Metrics.Values)
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
            var eventCounts = execution.Eventcounts;
            foreach (var eventType in ALL_COMMAND_EVENT_TYPES)
            {
                long eventCount = eventCounts.GetCount(eventType);
                var threadPoolEventType = ThreadPoolEventTypeHelper.From(eventType);
                if (threadPoolEventType != ThreadPoolEventType.UNKNOWN)
                {
                    var ordinal = (long)threadPoolEventType;
                    initialCountArray[ordinal] += eventCount;
                }
            }

            return initialCountArray;
        };

        public static Func<long[], long[], long[]> CounterAggregator { get; } = (cumulativeEvents, bucketEventCounts) =>
        {
            for (var i = 0; i < NUMBER_THREADPOOL_EVENT_TYPES; i++)
            {
                cumulativeEvents[i] += bucketEventCounts[i];
            }

            return cumulativeEvents;
        };

        internal static void Reset()
        {
            RollingThreadPoolEventCounterStream.Reset();
            CumulativeThreadPoolEventCounterStream.Reset();
            RollingThreadPoolMaxConcurrencyStream.Reset();

            Metrics.Clear();
        }

        private readonly AtomicInteger _concurrentExecutionCount = new ();

        private readonly RollingThreadPoolEventCounterStream _rollingCounterStream;
        private readonly CumulativeThreadPoolEventCounterStream _cumulativeCounterStream;
        private readonly RollingThreadPoolMaxConcurrencyStream _rollingThreadPoolMaxConcurrencyStream;

        private HystrixThreadPoolMetrics(IHystrixThreadPoolKey threadPoolKey, IHystrixTaskScheduler threadPool, IHystrixThreadPoolOptions properties)
            : base(null)
        {
            ThreadPoolKey = threadPoolKey;
            TaskScheduler = threadPool;
            Properties = properties;

            _rollingCounterStream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, properties);
            _cumulativeCounterStream = CumulativeThreadPoolEventCounterStream.GetInstance(threadPoolKey, properties);
            _rollingThreadPoolMaxConcurrencyStream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, properties);
        }

        public static Func<int> GetCurrentConcurrencyThunk(IHystrixThreadPoolKey threadPoolKey)
        {
            return () =>
            {
                return GetInstance(threadPoolKey)._concurrentExecutionCount.Value;
            };
        }

        public IHystrixTaskScheduler TaskScheduler { get; }

        public IHystrixThreadPoolKey ThreadPoolKey { get; }

        public IHystrixThreadPoolOptions Properties { get; }

        public int CurrentActiveCount => TaskScheduler.CurrentActiveCount;

        public int CurrentCompletedTaskCount => TaskScheduler.CurrentCompletedTaskCount;

        public int CurrentCorePoolSize => TaskScheduler.CurrentCorePoolSize;

        public int CurrentLargestPoolSize => TaskScheduler.CurrentLargestPoolSize;

        public int CurrentMaximumPoolSize => TaskScheduler.CurrentMaximumPoolSize;

        public int CurrentPoolSize => TaskScheduler.CurrentPoolSize;

        public int CurrentTaskCount => TaskScheduler.CurrentTaskCount;

        public int CurrentQueueSize => TaskScheduler.CurrentQueueSize;

        public void MarkThreadExecution()
        {
            _concurrentExecutionCount.IncrementAndGet();
        }

        public long RollingCountThreadsExecuted => _rollingCounterStream.GetLatestCount(ThreadPoolEventType.EXECUTED);

        public long CumulativeCountThreadsExecuted => _cumulativeCounterStream.GetLatestCount(ThreadPoolEventType.EXECUTED);

        public long RollingCountThreadsRejected => _rollingCounterStream.GetLatestCount(ThreadPoolEventType.REJECTED);

        public long CumulativeCountThreadsRejected => _cumulativeCounterStream.GetLatestCount(ThreadPoolEventType.REJECTED);

        public long GetRollingCount(ThreadPoolEventType @event)
        {
            return _rollingCounterStream.GetLatestCount(@event);
        }

        public override long GetRollingCount(HystrixRollingNumberEvent @event)
        {
            return _rollingCounterStream.GetLatestCount(ThreadPoolEventTypeHelper.From(@event));
        }

        public long GetCumulativeCount(ThreadPoolEventType @event)
        {
            return _cumulativeCounterStream.GetLatestCount(@event);
        }

        public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
        {
            return _cumulativeCounterStream.GetLatestCount(ThreadPoolEventTypeHelper.From(@event));
        }

        public void MarkThreadCompletion()
        {
            _concurrentExecutionCount.DecrementAndGet();
        }

        public long RollingMaxActiveThreads
        {
            get { return _rollingThreadPoolMaxConcurrencyStream.LatestRollingMax; }
        }

        public void MarkThreadRejection()
        {
            _concurrentExecutionCount.DecrementAndGet();
        }
    }
}
