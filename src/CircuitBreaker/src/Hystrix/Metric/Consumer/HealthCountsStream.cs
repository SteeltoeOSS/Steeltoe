// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class HealthCountsStream : BucketedRollingCounterStream<HystrixCommandCompletion, long[], HealthCounts>
{
    private static readonly ConcurrentDictionary<string, HealthCountsStream> Streams = new();

    private static readonly int NumEventTypes = HystrixEventTypeHelper.Values.Count;

    private static Func<HealthCounts, long[], HealthCounts> HealthCheckAccumulator { get; } = (healthCounts, bucketEventCounts) =>
        healthCounts.Plus(bucketEventCounts);

    public override long[] EmptyBucketSummary => new long[NumEventTypes];

    public override HealthCounts EmptyOutputValue => HealthCounts.Empty;

    private HealthCountsStream(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs,
        Func<long[], HystrixCommandCompletion, long[]> reduceCommandCompletion)
        : base(HystrixCommandCompletionStream.GetInstance(commandKey), numBuckets, bucketSizeInMs, reduceCommandCompletion, HealthCheckAccumulator)
    {
    }

    public static HealthCountsStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
    {
        int healthCountBucketSizeInMs = properties.MetricsHealthSnapshotIntervalInMilliseconds;

        if (healthCountBucketSizeInMs == 0)
        {
            throw new ArgumentOutOfRangeException(
                "You have set the bucket size to 0ms.  Please set a positive number, so that the metric stream can be properly consumed");
        }

        int numHealthCountBuckets = properties.MetricsRollingStatisticalWindowInMilliseconds / healthCountBucketSizeInMs;

        return GetInstance(commandKey, numHealthCountBuckets, healthCountBucketSizeInMs);
    }

    public static HealthCountsStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
    {
        HealthCountsStream result = Streams.GetOrAddEx(commandKey.Name, _ =>
        {
            var newStream = new HealthCountsStream(commandKey, numBuckets, bucketSizeInMs, HystrixCommandMetrics.AppendEventToBucket);
            newStream.StartCachingStreamValuesIfUnstarted();
            return newStream;
        });

        return result;
    }

    public static void Reset()
    {
        foreach (HealthCountsStream stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixCommandCompletionStream.Reset();

        Streams.Clear();
    }

    public static void RemoveByKey(IHystrixCommandKey key)
    {
        Streams.TryRemove(key.Name, out _);
    }

    internal static HealthCountsStream GetInstance(string commandKey)
    {
        Streams.TryGetValue(commandKey, out HealthCountsStream result);
        return result;
    }
}
