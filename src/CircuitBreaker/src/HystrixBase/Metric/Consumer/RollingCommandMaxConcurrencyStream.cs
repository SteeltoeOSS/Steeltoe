// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingCommandMaxConcurrencyStream : RollingConcurrencyStream
{
    private static readonly ConcurrentDictionary<string, RollingCommandMaxConcurrencyStream> Streams = new ();

    public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
    {
        var counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
        var numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
        var counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

        return GetInstance(commandKey, numCounterBuckets, counterBucketSizeInMs);
    }

    public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
    {
        var result = Streams.GetOrAddEx(commandKey.Name, _ =>
        {
            var stream = new RollingCommandMaxConcurrencyStream(commandKey, numBuckets, bucketSizeInMs);
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

        HystrixCommandStartStream.Reset();

        Streams.Clear();
    }

    private RollingCommandMaxConcurrencyStream(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        : base(HystrixCommandStartStream.GetInstance(commandKey), numBuckets, bucketSizeInMs)
    {
    }
}
