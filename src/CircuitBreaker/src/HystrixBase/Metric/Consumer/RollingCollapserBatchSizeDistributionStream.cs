// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using HdrHistogram;
using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingCollapserBatchSizeDistributionStream : RollingDistributionStream<HystrixCollapserEvent>
    {
        private static readonly ConcurrentDictionary<string, RollingCollapserBatchSizeDistributionStream> Streams = new ConcurrentDictionary<string, RollingCollapserBatchSizeDistributionStream>();

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
            int percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
            int numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
            int percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

            return GetInstance(collapserKey, numPercentileBuckets, percentileBucketSizeInMs);
        }

        public static RollingCollapserBatchSizeDistributionStream GetInstance(IHystrixCollapserKey collapserKey, int numBuckets, int bucketSizeInMs)
        {
            return Streams.GetOrAddEx(collapserKey.Name, (k) =>
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
