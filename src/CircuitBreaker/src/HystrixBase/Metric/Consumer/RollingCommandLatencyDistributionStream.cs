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
using Steeltoe.CircuitBreaker.Hystrix.Util;
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
            int percentileMetricWindow = properties.MetricsRollingPercentileWindowInMilliseconds;
            int numPercentileBuckets = properties.MetricsRollingPercentileWindowBuckets;
            int percentileBucketSizeInMs = percentileMetricWindow / numPercentileBuckets;

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
