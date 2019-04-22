//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;


namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class CumulativeCollapserEventCounterStream : BucketedCumulativeCounterStream<HystrixCollapserEvent, long[], long[]>
    {
        private static readonly ConcurrentDictionary<string, CumulativeCollapserEventCounterStream> streams = new ConcurrentDictionary<string, CumulativeCollapserEventCounterStream>();

        private static readonly int NUM_EVENT_TYPES = CollapserEventTypeHelper.Values.Count;

        public static CumulativeCollapserEventCounterStream GetInstance(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions properties)
        {
            int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(collapserKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static CumulativeCollapserEventCounterStream GetInstance(IHystrixCollapserKey collapserKey, int numBuckets, int bucketSizeInMs)
        {
            return streams.GetOrAddEx(collapserKey.Name, (k) => new CumulativeCollapserEventCounterStream(collapserKey, numBuckets, bucketSizeInMs, HystrixCollapserMetrics.AppendEventToBucket, HystrixCollapserMetrics.BucketAggregator));

        }

        public static void Reset()
        {
            streams.Clear();
        }

        private CumulativeCollapserEventCounterStream(IHystrixCollapserKey collapserKey, int numCounterBuckets, int counterBucketSizeInMs, Func<long[], HystrixCollapserEvent, long[]> appendEventToBucket, Func<long[], long[], long[]> reduceBucket)
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
