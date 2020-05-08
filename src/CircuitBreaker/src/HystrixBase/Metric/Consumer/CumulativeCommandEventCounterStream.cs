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
    public class CumulativeCommandEventCounterStream : BucketedCumulativeCounterStream<HystrixCommandCompletion, long[], long[]>
    {
        private static readonly ConcurrentDictionary<string, CumulativeCommandEventCounterStream> Streams = new ConcurrentDictionary<string, CumulativeCommandEventCounterStream>();

        private static readonly int NUM_EVENT_TYPES = HystrixEventTypeHelper.Values.Count;

        public static CumulativeCommandEventCounterStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
        {
            var counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            var numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            var counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(commandKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static CumulativeCommandEventCounterStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        {
            var result = Streams.GetOrAddEx(commandKey.Name, (k) =>
            {
                var stream = new CumulativeCommandEventCounterStream(commandKey, numBuckets, bucketSizeInMs, HystrixCommandMetrics.AppendEventToBucket, HystrixCommandMetrics.BucketAggregator);
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

            HystrixCommandCompletionStream.Reset();

            Streams.Clear();
        }

        private CumulativeCommandEventCounterStream(IHystrixCommandKey commandKey, int numCounterBuckets, int counterBucketSizeInMs, Func<long[], HystrixCommandCompletion, long[]> reduceCommandCompletion, Func<long[], long[], long[]> reduceBucket)
                    : base(HystrixCommandCompletionStream.GetInstance(commandKey), numCounterBuckets, counterBucketSizeInMs, reduceCommandCompletion, reduceBucket)
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

        public long GetLatest(HystrixEventType eventType)
        {
            return Latest[(int)eventType];
        }
    }
}
