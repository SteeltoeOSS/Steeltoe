// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingCommandEventCounterStream : BucketedRollingCounterStream<HystrixCommandCompletion, long[], long[]>
{
    private static readonly ConcurrentDictionary<string, RollingCommandEventCounterStream> Streams = new ();

    private static readonly int NUM_EVENT_TYPES = HystrixEventTypeHelper.Values.Count;

    public static RollingCommandEventCounterStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
    {
        var counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
        var numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
        var counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

        return GetInstance(commandKey, numCounterBuckets, counterBucketSizeInMs);
    }

    public static RollingCommandEventCounterStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
    {
        var result = Streams.GetOrAddEx(commandKey.Name, _ =>
        {
            var stream = new RollingCommandEventCounterStream(commandKey, numBuckets, bucketSizeInMs, HystrixCommandMetrics.AppendEventToBucket, HystrixCommandMetrics.BucketAggregator);
            stream.StartCachingStreamValuesIfUnstarted();
            return stream;
        });
        return result;
    }

    public static void Reset()
    {
        foreach (var stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixCommandCompletionStream.Reset();
        Streams.Clear();
    }

    private RollingCommandEventCounterStream(IHystrixCommandKey commandKey, int numCounterBuckets, int counterBucketSizeInMs, Func<long[], HystrixCommandCompletion, long[]> reduceCommandCompletion, Func<long[], long[], long[]> reduceBucket)
        : base(HystrixCommandCompletionStream.GetInstance(commandKey), numCounterBuckets, counterBucketSizeInMs, reduceCommandCompletion, reduceBucket)
    {
    }

    public override long[] EmptyBucketSummary
    {
        get { return new long[NUM_EVENT_TYPES]; }
    }

    public override long[] EmptyOutputValue
    {
        get { return new long[NUM_EVENT_TYPES]; }
    }

    public long GetLatest(HystrixEventType eventType)
    {
        return Latest[(int)eventType];
    }
}
