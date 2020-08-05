// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using Steeltoe.Common;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingCommandLatencyDistributionStream : RollingDistributionStream<HystrixCommandCompletion>
    {
        private static readonly ConcurrentDictionary<string, RollingCommandLatencyDistributionStream> Streams = new ConcurrentDictionary<string, RollingCommandLatencyDistributionStream>();

        private static Func<LongHistogram, HystrixCommandCompletion, LongHistogram> AddValuesToBucket { get; } = (initialDistribution, @event) =>
        {
            if (@event.DidCommandExecute && @event.ExecutionLatency > -1)
            {
                initialDistribution.RecordValue(@event.ExecutionLatency);
            }

            return initialDistribution;
        };

        public static RollingCommandLatencyDistributionStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
        {
            var percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
            var numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
            var percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

            return GetInstance(commandKey, numPercentileBuckets, percentileBucketSizeInMs);
        }

        public static RollingCommandLatencyDistributionStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        {
            var result = Streams.GetOrAddEx(commandKey.Name, (k) =>
            {
                var stream = new RollingCommandLatencyDistributionStream(commandKey, numBuckets, bucketSizeInMs);
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

        private RollingCommandLatencyDistributionStream(IHystrixCommandKey commandKey, int numPercentileBuckets, int percentileBucketSizeInMs)
            : base(HystrixCommandCompletionStream.GetInstance(commandKey), numPercentileBuckets, percentileBucketSizeInMs, AddValuesToBucket)
        {
        }
    }
}
