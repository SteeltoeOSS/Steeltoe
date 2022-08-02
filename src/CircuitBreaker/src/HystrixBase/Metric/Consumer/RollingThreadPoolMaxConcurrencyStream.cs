// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingThreadPoolMaxConcurrencyStream : RollingConcurrencyStream
{
    private static readonly ConcurrentDictionary<string, RollingThreadPoolMaxConcurrencyStream> Streams = new();

    public RollingThreadPoolMaxConcurrencyStream(IHystrixThreadPoolKey threadPoolKey, int numBuckets, int bucketSizeInMs)
        : base(HystrixThreadPoolStartStream.GetInstance(threadPoolKey), numBuckets, bucketSizeInMs)
    {
    }

    public static RollingThreadPoolMaxConcurrencyStream GetInstance(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions properties)
    {
        int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
        int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
        int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

        return GetInstance(threadPoolKey, numCounterBuckets, counterBucketSizeInMs);
    }

    public static RollingThreadPoolMaxConcurrencyStream GetInstance(IHystrixThreadPoolKey threadPoolKey, int numBuckets, int bucketSizeInMs)
    {
        return Streams.GetOrAddEx(threadPoolKey.Name, _ =>
        {
            var stream = new RollingThreadPoolMaxConcurrencyStream(threadPoolKey, numBuckets, bucketSizeInMs);
            stream.StartCachingStreamValuesIfUnstarted();
            return stream;
        });
    }

    public static void Reset()
    {
        foreach (RollingThreadPoolMaxConcurrencyStream stream in Streams.Values)
        {
            stream.Unsubscribe();
        }

        HystrixThreadPoolStartStream.Reset();

        Streams.Clear();
    }
}
