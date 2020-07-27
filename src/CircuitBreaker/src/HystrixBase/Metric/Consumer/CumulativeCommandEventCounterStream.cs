// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
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
