// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingCollapserBatchSizeDistributionStream : RollingDistributionStream<HystrixCollapserEvent>
    {
        private static readonly ConcurrentDictionary<string, RollingCollapserBatchSizeDistributionStream> Streams = new ();

        private static Func<LongHistogram, HystrixCollapserEvent, LongHistogram> AddValuesToBucket { get; } = (initialDistribution, @event) =>
        {
            switch (@event.EventType)
            {
                case CollapserEventType.ADDED_TO_BATCH:
                    if (@event.Count > -1)
                    {
                        initialDistribution.RecordValue(@event.Count);
                    }

                    break;
                default:
                    // do nothing
                    break;
            }

            return initialDistribution;
        };

        public static RollingCollapserBatchSizeDistributionStream GetInstance(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions properties)
        {
            var percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
            var numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
            var percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

            return GetInstance(collapserKey, numPercentileBuckets, percentileBucketSizeInMs);
        }

        public static RollingCollapserBatchSizeDistributionStream GetInstance(IHystrixCollapserKey collapserKey, int numBuckets, int bucketSizeInMs)
        {
            return Streams.GetOrAddEx(collapserKey.Name, k =>
            {
                var stream = new RollingCollapserBatchSizeDistributionStream(collapserKey, numBuckets, bucketSizeInMs);
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

            HystrixCollapserEventStream.Reset();
            Streams.Clear();
        }

        private RollingCollapserBatchSizeDistributionStream(IHystrixCollapserKey collapserKey, int numPercentileBuckets, int percentileBucketSizeInMs)
            : base(HystrixCollapserEventStream.GetInstance(collapserKey), numPercentileBuckets, percentileBucketSizeInMs, AddValuesToBucket)
        {
        }
    }
}
