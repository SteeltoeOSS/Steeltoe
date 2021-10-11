// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class HealthCountsStream : BucketedRollingCounterStream<HystrixCommandCompletion, long[], HealthCounts>
    {
        private static readonly ConcurrentDictionary<string, HealthCountsStream> Streams = new ();

        private static readonly int NUM_EVENT_TYPES = HystrixEventTypeHelper.Values.Count;

        private static Func<HealthCounts, long[], HealthCounts> HealthCheckAccumulator { get; } = (healthCounts, bucketEventCounts) =>
       {
           return healthCounts.Plus(bucketEventCounts);
       };

        public static HealthCountsStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
        {
            var healthCountBucketSizeInMs = properties.MetricsHealthSnapshotIntervalInMilliseconds;
            if (healthCountBucketSizeInMs == 0)
            {
                throw new ArgumentOutOfRangeException("You have set the bucket size to 0ms.  Please set a positive number, so that the metric stream can be properly consumed");
            }

            var numHealthCountBuckets = properties.MetricsRollingStatisticalWindowInMilliseconds / healthCountBucketSizeInMs;

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
            foreach (var stream in Streams.Values)
            {
                stream.Unsubscribe();
            }

            HystrixCommandCompletionStream.Reset();

            Streams.Clear();
        }

        public static void RemoveByKey(IHystrixCommandKey key)
        {
            Streams.TryRemove(key.Name, out _);
        }

        internal static HealthCountsStream GetInstance(string commandKey)
        {
            Streams.TryGetValue(commandKey, out var result);
            return result;
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
