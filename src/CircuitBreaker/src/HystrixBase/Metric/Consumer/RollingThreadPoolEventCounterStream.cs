// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingThreadPoolEventCounterStream : BucketedRollingCounterStream<HystrixCommandCompletion, long[], long[]>
    {
        private static readonly ConcurrentDictionary<string, RollingThreadPoolEventCounterStream> Streams = new ConcurrentDictionary<string, RollingThreadPoolEventCounterStream>();

        private static readonly int ALL_EVENT_TYPES_SIZE = ThreadPoolEventTypeHelper.Values.Count;

        public static RollingThreadPoolEventCounterStream GetInstance(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions properties)
        {
            int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(threadPoolKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static RollingThreadPoolEventCounterStream GetInstance(IHystrixThreadPoolKey threadPoolKey, int numBuckets, int bucketSizeInMs)
        {
            return Streams.GetOrAddEx(threadPoolKey.Name, (k) =>
            {
                var stream = new RollingThreadPoolEventCounterStream(threadPoolKey, numBuckets, bucketSizeInMs, HystrixThreadPoolMetrics.AppendEventToBucket, HystrixThreadPoolMetrics.CounterAggregator);
                stream.StartCachingStreamValuesIfUnstarted();
                return stream;
            });
        }

        public static void Reset()
        {
            foreach (var stream in Streams.Values)
            {
                stream.Unsubscribe();
            }

            HystrixThreadPoolCompletionStream.Reset();

            Streams.Clear();
        }

        private RollingThreadPoolEventCounterStream(IHystrixThreadPoolKey threadPoolKey, int numCounterBuckets, int counterBucketSizeInMs, Func<long[], HystrixCommandCompletion, long[]> reduceCommandCompletion, Func<long[], long[], long[]> reduceBucket)
         : base(HystrixThreadPoolCompletionStream.GetInstance(threadPoolKey), numCounterBuckets, counterBucketSizeInMs, reduceCommandCompletion, reduceBucket)
        {
        }

        public override long[] EmptyBucketSummary
        {
            get { return new long[ALL_EVENT_TYPES_SIZE]; }
        }

        public override long[] EmptyOutputValue
        {
            get { return new long[ALL_EVENT_TYPES_SIZE]; }
        }

        public long GetLatestCount(ThreadPoolEventType eventType)
        {
            return Latest[(int)eventType];
        }
    }
}
