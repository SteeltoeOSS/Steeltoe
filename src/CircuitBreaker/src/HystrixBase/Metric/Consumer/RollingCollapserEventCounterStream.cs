// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingCollapserEventCounterStream : BucketedRollingCounterStream<HystrixCollapserEvent, long[], long[]>
    {
        private static readonly ConcurrentDictionary<string, RollingCollapserEventCounterStream> Streams = new ConcurrentDictionary<string, RollingCollapserEventCounterStream>();

        private static readonly int NUM_EVENT_TYPES = CollapserEventTypeHelper.Values.Count;

        public static RollingCollapserEventCounterStream GetInstance(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions properties)
        {
            int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(collapserKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static RollingCollapserEventCounterStream GetInstance(IHystrixCollapserKey collapserKey, int numBuckets, int bucketSizeInMs)
        {
            var result = Streams.GetOrAddEx(collapserKey.Name, (k) =>
            {
                var stream = new RollingCollapserEventCounterStream(collapserKey, numBuckets, bucketSizeInMs, HystrixCollapserMetrics.AppendEventToBucket, HystrixCollapserMetrics.BucketAggregator);
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

            HystrixCollapserEventStream.Reset();

            Streams.Clear();
        }

        private RollingCollapserEventCounterStream(
            IHystrixCollapserKey collapserKey,
            int numCounterBuckets,
            int counterBucketSizeInMs,
            Func<long[], HystrixCollapserEvent, long[]> appendEventToBucket,
            Func<long[], long[], long[]> reduceBucket)
            : base(HystrixCollapserEventStream.GetInstance(collapserKey), numCounterBuckets, counterBucketSizeInMs, appendEventToBucket, reduceBucket)
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

        public long GetLatest(CollapserEventType eventType)
        {
            return Latest[(int)eventType];
        }
    }
}
