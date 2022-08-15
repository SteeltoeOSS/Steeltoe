// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using HdrHistogram;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingCollapserBatchSizeDistributionStream : RollingDistributionStream<HystrixCollapserEvent>
{
    private static readonly ConcurrentDictionary<string, RollingCollapserBatchSizeDistributionStream> Streams = new();

    private static Func<LongHistogram, HystrixCollapserEvent, LongHistogram> AddValuesToBucket { get; } = (initialDistribution, @event) =>
    {
        switch (@event.EventType)
        {
            case CollapserEventType.AddedToBatch:
                if (@event.Count > -1)
                {
                    initialDistribution.RecordValue(@event.Count);
                }

                break;
        }

        return initialDistribution;
    };

    private RollingCollapserBatchSizeDistributionStream(IHystrixCollapserKey collapserKey, int numPercentileBuckets, int percentileBucketSizeInMs)
        : base(HystrixCollapserEventStream.GetInstance(collapserKey), numPercentileBuckets, percentileBucketSizeInMs, AddValuesToBucket)
    {
    }

    public static RollingCollapserBatchSizeDistributionStream GetInstance(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions properties)
    {
        int percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
        int numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
        int percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

        return GetInstance(collapserKey, numPercentileBuckets, percentileBucketSizeInMs);
    }

    public static RollingCollapserBatchSizeDistributionStream GetInstance(IHystrixCollapserKey collapserKey, int numBuckets, int bucketSizeInMs)
    {
        return Streams.GetOrAddEx(collapserKey.Name, _ =>
        {
            var stream = new RollingCollapserBatchSizeDistributionStream(collapserKey, numBuckets, bucketSizeInMs);
            stream.StartCachingStreamValuesIfUnstarted();
            return stream;
        });
    }

    public static void Reset()
    {
        foreach (RollingCollapserBatchSizeDistributionStream stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixCollapserEventStream.Reset();
        Streams.Clear();
    }
}
