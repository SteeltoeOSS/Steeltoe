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

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class HealthCountsStream : BucketedRollingCounterStream<HystrixCommandCompletion, long[], HealthCounts>
    {
        private static readonly ConcurrentDictionary<string, HealthCountsStream> Streams = new ConcurrentDictionary<string, HealthCountsStream>();

        private static readonly int NUM_EVENT_TYPES = HystrixEventTypeHelper.Values.Count;

        private static Func<HealthCounts, long[], HealthCounts> HealthCheckAccumulator { get; } = (healthCounts, bucketEventCounts) =>
       {
           return healthCounts.Plus(bucketEventCounts);
       };

        public static HealthCountsStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
        {
            int healthCountBucketSizeInMs = properties.MetricsHealthSnapshotIntervalInMilliseconds;
            if (healthCountBucketSizeInMs == 0)
            {
                throw new ArgumentOutOfRangeException("You have set the bucket size to 0ms.  Please set a positive number, so that the metric stream can be properly consumed");
            }

            int numHealthCountBuckets = properties.MetricsRollingStatisticalWindowInMilliseconds / healthCountBucketSizeInMs;

            return GetInstance(commandKey, numHealthCountBuckets, healthCountBucketSizeInMs);
        }

        public static HealthCountsStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        {
            var result = Streams.GetOrAddEx(commandKey.Name, (k) =>
            {
                var newStream = new HealthCountsStream(commandKey, numBuckets, bucketSizeInMs, HystrixCommandMetrics.AppendEventToBucket);
                newStream.StartCachingStreamValuesIfUnstarted();
                return newStream;
            });

            return result;
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        public static void RemoveByKey(IHystrixCommandKey key)
        {
            Streams.TryRemove(key.Name, out HealthCountsStream old);
        }

        private HealthCountsStream(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs, Func<long[], HystrixCommandCompletion, long[]> reduceCommandCompletion)
                    : base(HystrixCommandCompletionStream.GetInstance(commandKey), numBuckets, bucketSizeInMs, reduceCommandCompletion, HealthCheckAccumulator)
        {
        }

        public override long[] EmptyBucketSummary
        {
            get { return new long[NUM_EVENT_TYPES]; }
        }

        public override HealthCounts EmptyOutputValue
        {
            get { return HealthCounts.Empty; }
        }
    }
}
