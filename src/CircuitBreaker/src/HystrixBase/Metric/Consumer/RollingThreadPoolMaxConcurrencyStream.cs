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
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingThreadPoolMaxConcurrencyStream : RollingConcurrencyStream
    {
        private static readonly ConcurrentDictionary<string, RollingThreadPoolMaxConcurrencyStream> Streams = new ConcurrentDictionary<string, RollingThreadPoolMaxConcurrencyStream>();

        public static RollingThreadPoolMaxConcurrencyStream GetInstance(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions properties)
        {
            int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(threadPoolKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static RollingThreadPoolMaxConcurrencyStream GetInstance(IHystrixThreadPoolKey threadPoolKey, int numBuckets, int bucketSizeInMs)
        {
            return Streams.GetOrAddEx(threadPoolKey.Name, (k) =>
            {
                var stream = new RollingThreadPoolMaxConcurrencyStream(threadPoolKey, numBuckets, bucketSizeInMs);
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

            HystrixThreadPoolStartStream.Reset();

            Streams.Clear();
        }

        public RollingThreadPoolMaxConcurrencyStream(IHystrixThreadPoolKey threadPoolKey, int numBuckets, int bucketSizeInMs)
        : base(HystrixThreadPoolStartStream.GetInstance(threadPoolKey), numBuckets, bucketSizeInMs)
        {
        }
    }
}
