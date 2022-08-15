// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingCommandMaxConcurrencyStream : RollingConcurrencyStream
{
    private static readonly ConcurrentDictionary<string, RollingCommandMaxConcurrencyStream> Streams = new();

    private RollingCommandMaxConcurrencyStream(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        : base(HystrixCommandStartStream.GetInstance(commandKey), numBuckets, bucketSizeInMs)
    {
    }

    public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
    {
        int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
        int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
        int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

        return GetInstance(commandKey, numCounterBuckets, counterBucketSizeInMs);
    }

    public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
    {
        RollingCommandMaxConcurrencyStream result = Streams.GetOrAddEx(commandKey.Name, _ =>
        {
            var stream = new RollingCommandMaxConcurrencyStream(commandKey, numBuckets, bucketSizeInMs);
            stream.StartCachingStreamValuesIfUnstarted();
            return stream;
        });

        return result;
    }

    public static void Reset()
    {
        foreach (RollingCommandMaxConcurrencyStream stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixCommandStartStream.Reset();

        Streams.Clear();
    }
}
