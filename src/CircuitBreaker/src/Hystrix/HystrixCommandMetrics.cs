// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommandMetrics : HystrixMetrics
{
    private static readonly IList<HystrixEventType> AllEventTypes = HystrixEventTypeHelper.Values;

    private static readonly ConcurrentDictionary<string, HystrixCommandMetrics> Metrics = new();

    private readonly AtomicInteger _concurrentExecutionCount = new();

    private readonly RollingCommandEventCounterStream _rollingCommandEventCounterStream;
    private readonly CumulativeCommandEventCounterStream _cumulativeCommandEventCounterStream;
    private readonly RollingCommandLatencyDistributionStream _rollingCommandLatencyDistributionStream;
    private readonly RollingCommandUserLatencyDistributionStream _rollingCommandUserLatencyDistributionStream;
    private readonly RollingCommandMaxConcurrencyStream _rollingCommandMaxConcurrencyStream;

    private readonly object _syncLock = new();

    private HealthCountsStream _healthCountsStream;

    public static Func<long[], HystrixCommandCompletion, long[]> AppendEventToBucket { get; } = (initialCountArray, execution) =>
    {
        ExecutionResult.EventCounts eventCounts = execution.Eventcounts;

        foreach (HystrixEventType eventType in AllEventTypes)
        {
            switch (eventType)
            {
                case HystrixEventType.ExceptionThrown:
                    break; // this is just a sum of other anyway - don't do the work here
                default:
                    int ordinal = (int)eventType;
                    initialCountArray[ordinal] += eventCounts.GetCount(eventType);
                    break;
            }
        }

        return initialCountArray;
    };

    public static Func<long[], long[], long[]> BucketAggregator { get; } = (cumulativeEvents, bucketEventCounts) =>
    {
        foreach (HystrixEventType eventType in AllEventTypes)
        {
            switch (eventType)
            {
                case HystrixEventType.ExceptionThrown:
                    foreach (HystrixEventType exceptionEventType in HystrixEventTypeHelper.ExceptionProducingEventTypes)
                    {
                        int ordinal1 = (int)eventType;
                        int ordinal2 = (int)exceptionEventType;
                        cumulativeEvents[ordinal1] += bucketEventCounts[ordinal2];
                    }

                    break;
                default:
                    int ordinal = (int)eventType;
                    cumulativeEvents[ordinal] += bucketEventCounts[ordinal];
                    break;
            }
        }

        return cumulativeEvents;
    };

    public IHystrixCommandKey CommandKey { get; }

    public IHystrixCommandGroupKey CommandGroup { get; }

    public IHystrixThreadPoolKey ThreadPoolKey { get; }

    public IHystrixCommandOptions Properties { get; }

    public int ExecutionTimeMean => _rollingCommandLatencyDistributionStream.LatestMean;

    public int TotalTimeMean => _rollingCommandUserLatencyDistributionStream.LatestMean;

    public long RollingMaxConcurrentExecutions => _rollingCommandMaxConcurrencyStream.LatestRollingMax;

    public int CurrentConcurrentExecutionCount => _concurrentExecutionCount.Value;

    public HealthCounts HealthCounts => _healthCountsStream.Latest;

    internal HystrixCommandMetrics(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixThreadPoolKey threadPoolKey,
        IHystrixCommandOptions properties)
        : base(null)
    {
        CommandKey = key;
        CommandGroup = commandGroup;
        ThreadPoolKey = threadPoolKey;
        Properties = properties;

        _healthCountsStream = HealthCountsStream.GetInstance(key, properties);
        _rollingCommandEventCounterStream = RollingCommandEventCounterStream.GetInstance(key, properties);
        _cumulativeCommandEventCounterStream = CumulativeCommandEventCounterStream.GetInstance(key, properties);

        _rollingCommandLatencyDistributionStream = RollingCommandLatencyDistributionStream.GetInstance(key, properties);
        _rollingCommandUserLatencyDistributionStream = RollingCommandUserLatencyDistributionStream.GetInstance(key, properties);
        _rollingCommandMaxConcurrencyStream = RollingCommandMaxConcurrencyStream.GetInstance(key, properties);
    }

    public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixCommandOptions properties)
    {
        return GetInstance(key, commandGroup, null, properties);
    }

    public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey commandGroup, IHystrixThreadPoolKey threadPoolKey,
        IHystrixCommandOptions properties)
    {
        // attempt to retrieve from cache first
        IHystrixThreadPoolKey nonNullThreadPoolKey = threadPoolKey ?? HystrixThreadPoolKeyDefault.AsKey(commandGroup.Name);

        return Metrics.GetOrAddEx(key.Name, _ => new HystrixCommandMetrics(key, commandGroup, nonNullThreadPoolKey, properties));
    }

    public static HystrixCommandMetrics GetInstance(IHystrixCommandKey key)
    {
        Metrics.TryGetValue(key.Name, out HystrixCommandMetrics result);
        return result;
    }

    public static ICollection<HystrixCommandMetrics> GetInstances()
    {
        var commandMetrics = new List<HystrixCommandMetrics>();

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

    /* package */
    internal void ResetStream()
    {
        lock (_syncLock)
        {
            _healthCountsStream.Unsubscribe();
            HealthCountsStream.RemoveByKey(CommandKey);
            _healthCountsStream = HealthCountsStream.GetInstance(CommandKey, Properties);
        }
    }

    public long GetRollingCount(HystrixEventType eventType)
    {
        return _rollingCommandEventCounterStream.GetLatest(eventType);
    }

    public override long GetRollingCount(HystrixRollingNumberEvent @event)
    {
        return GetRollingCount(HystrixEventTypeHelper.From(@event));
    }

    public long GetCumulativeCount(HystrixEventType eventType)
    {
        return _cumulativeCommandEventCounterStream.GetLatest(eventType);
    }

    public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
    {
        return GetCumulativeCount(HystrixEventTypeHelper.From(@event));
    }

    public int GetExecutionTimePercentile(double percentile)
    {
        return _rollingCommandLatencyDistributionStream.GetLatestPercentile(percentile);
    }

    public int GetTotalTimePercentile(double percentile)
    {
        return _rollingCommandUserLatencyDistributionStream.GetLatestPercentile(percentile);
    }

    internal void MarkCommandStart(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy)
    {
        int currentCount = _concurrentExecutionCount.IncrementAndGet();
        HystrixThreadEventStream.GetInstance().CommandExecutionStarted(commandKey, threadPoolKey, isolationStrategy, currentCount);
    }

    internal void MarkCommandDone(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, bool executionStarted)
    {
        HystrixThreadEventStream.GetInstance().ExecutionDone(executionResult, commandKey, threadPoolKey);

        if (executionStarted)
        {
            _concurrentExecutionCount.DecrementAndGet();
        }
    }

    private void UnsubscribeAll()
    {
        _healthCountsStream.Unsubscribe();
        _rollingCommandEventCounterStream.Unsubscribe();
        _cumulativeCommandEventCounterStream.Unsubscribe();
        _rollingCommandLatencyDistributionStream.Unsubscribe();
        _rollingCommandUserLatencyDistributionStream.Unsubscribe();
        _rollingCommandMaxConcurrencyStream.Unsubscribe();
    }
}
