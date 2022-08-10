// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using HdrHistogram;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingCommandUserLatencyDistributionStream : RollingDistributionStream<HystrixCommandCompletion>
{
    private static readonly ConcurrentDictionary<string, RollingCommandUserLatencyDistributionStream> Streams = new();

    private static Func<LongHistogram, HystrixCommandCompletion, LongHistogram> AddValuesToBucket { get; } = (initialDistribution, @event) =>
    {
        if (@event.DidCommandExecute && @event.TotalLatency > -1)
        {
            initialDistribution.RecordValue(@event.TotalLatency);
        }

        return initialDistribution;
    };

    private RollingCommandUserLatencyDistributionStream(IHystrixCommandKey commandKey, int numPercentileBuckets, int percentileBucketSizeInMs)
        : base(HystrixCommandCompletionStream.GetInstance(commandKey), numPercentileBuckets, percentileBucketSizeInMs, AddValuesToBucket)
    {
    }

    public static RollingCommandUserLatencyDistributionStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
    {
        int percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
        int numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
        int percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

        return GetInstance(commandKey, numPercentileBuckets, percentileBucketSizeInMs);
    }

    public static RollingCommandUserLatencyDistributionStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
    {
        RollingCommandUserLatencyDistributionStream result = Streams.GetOrAddEx(commandKey.Name, _ =>
        {
            var stream = new RollingCommandUserLatencyDistributionStream(commandKey, numBuckets, bucketSizeInMs);
            stream.StartCachingStreamValuesIfUnstarted();
            return stream;
        });

        return result;
    }

    public static void Reset()
    {
        foreach (RollingCommandUserLatencyDistributionStream stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixCommandCompletionStream.Reset();

        Streams.Clear();
    }
}
