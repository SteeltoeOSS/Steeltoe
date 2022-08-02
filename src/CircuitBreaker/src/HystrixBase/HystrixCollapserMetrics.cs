// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCollapserMetrics : HystrixMetrics
{
    private static readonly ConcurrentDictionary<string, HystrixCollapserMetrics> Metrics = new();

    private static readonly IList<CollapserEventType> AllEventTypes = CollapserEventTypeHelper.Values;

    private readonly RollingCollapserEventCounterStream _rollingCollapserEventCounterStream;
    private readonly CumulativeCollapserEventCounterStream _cumulativeCollapserEventCounterStream;
    private readonly RollingCollapserBatchSizeDistributionStream _rollingCollapserBatchSizeDistributionStream;

    public static Func<long[], HystrixCollapserEvent, long[]> AppendEventToBucket { get; } = (initialCountArray, collapserEvent) =>
    {
        CollapserEventType eventType = collapserEvent.EventType;
        int count = collapserEvent.Count;
        initialCountArray[(int)eventType] += count;
        return initialCountArray;
    };

    public static Func<long[], long[], long[]> BucketAggregator { get; } = (cumulativeEvents, bucketEventCounts) =>
    {
        foreach (CollapserEventType eventType in AllEventTypes)
        {
            cumulativeEvents[(int)eventType] += bucketEventCounts[(int)eventType];
        }

        return cumulativeEvents;
    };

    public IHystrixCollapserKey CollapserKey { get; }

    public IHystrixCollapserOptions Properties { get; }

    public int BatchSizeMean => _rollingCollapserBatchSizeDistributionStream.LatestMean;

    public int ShardSizeMean => 0;

    internal HystrixCollapserMetrics(IHystrixCollapserKey key, IHystrixCollapserOptions properties)
        : base(null)
    {
        CollapserKey = key;
        Properties = properties;

        _rollingCollapserEventCounterStream = RollingCollapserEventCounterStream.GetInstance(key, properties);
        _cumulativeCollapserEventCounterStream = CumulativeCollapserEventCounterStream.GetInstance(key, properties);
        _rollingCollapserBatchSizeDistributionStream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, properties);
    }

    public static HystrixCollapserMetrics GetInstance(IHystrixCollapserKey key, IHystrixCollapserOptions properties)
    {
        return Metrics.GetOrAddEx(key.Name, _ => new HystrixCollapserMetrics(key, properties));
    }

    public static ICollection<HystrixCollapserMetrics> GetInstances()
    {
        var collapserMetrics = new List<HystrixCollapserMetrics>();

        foreach (HystrixCollapserMetrics tpm in Metrics.Values)
        {
            collapserMetrics.Add(tpm);
        }

        return collapserMetrics.AsReadOnly();
    }

    internal static void Reset()
    {
        RollingCollapserEventCounterStream.Reset();
        CumulativeCollapserEventCounterStream.Reset();
        RollingCollapserBatchSizeDistributionStream.Reset();
        Metrics.Clear();
    }

    public long GetRollingCount(CollapserEventType collapserEventType)
    {
        return _rollingCollapserEventCounterStream.GetLatest(collapserEventType);
    }

    public override long GetRollingCount(HystrixRollingNumberEvent @event)
    {
        return GetRollingCount(CollapserEventTypeHelper.From(@event));
    }

    public long GetCumulativeCount(CollapserEventType collapserEventType)
    {
        return _cumulativeCollapserEventCounterStream.GetLatest(collapserEventType);
    }

    public override long GetCumulativeCount(HystrixRollingNumberEvent @event)
    {
        return GetCumulativeCount(CollapserEventTypeHelper.From(@event));
    }

    public int GetBatchSizePercentile(double percentile)
    {
        return _rollingCollapserBatchSizeDistributionStream.GetLatestPercentile(percentile);
    }

    public int GetShardSizePercentile(double percentile)
    {
        return 0;
    }

    public void MarkRequestBatched()
    {
        // for future use
    }

    public void MarkResponseFromCache()
    {
        HystrixThreadEventStream.GetInstance().CollapserResponseFromCache(CollapserKey);
    }

    public void MarkBatch(int batchSize)
    {
        HystrixThreadEventStream.GetInstance().CollapserBatchExecuted(CollapserKey, batchSize);
    }

    public void MarkShards(int numShards)
    {
        // for future use
    }
}
