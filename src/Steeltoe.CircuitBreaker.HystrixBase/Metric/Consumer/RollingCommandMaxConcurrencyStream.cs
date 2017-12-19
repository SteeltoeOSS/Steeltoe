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

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingCommandMaxConcurrencyStream : RollingConcurrencyStream
    {
        private static readonly ConcurrentDictionary<string, RollingCommandMaxConcurrencyStream> Streams = new ConcurrentDictionary<string, RollingCommandMaxConcurrencyStream>();

        public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, IHystrixCommandOptions properties)
        {
            int counterMetricWindow = properties.MetricsRollingStatisticalWindowInMilliseconds;
            int numCounterBuckets = properties.MetricsRollingStatisticalWindowBuckets;
            int counterBucketSizeInMs = counterMetricWindow / numCounterBuckets;

            return GetInstance(commandKey, numCounterBuckets, counterBucketSizeInMs);
        }

        public static RollingCommandMaxConcurrencyStream GetInstance(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
        {
            var result = Streams.GetOrAddEx(commandKey.Name, (k) => new RollingCommandMaxConcurrencyStream(commandKey, numBuckets, bucketSizeInMs));
            return result;
        }

        public static void Reset()
        {
            Streams.Clear();
        }

        private RollingCommandMaxConcurrencyStream(IHystrixCommandKey commandKey, int numBuckets, int bucketSizeInMs)
            : base(HystrixCommandStartStream.GetInstance(commandKey), numBuckets, bucketSizeInMs)
        {
        }
    }
}
