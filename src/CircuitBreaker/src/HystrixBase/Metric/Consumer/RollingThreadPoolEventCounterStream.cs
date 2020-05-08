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
            var counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            var numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            var counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

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
