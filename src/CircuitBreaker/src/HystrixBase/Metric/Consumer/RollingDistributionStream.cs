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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class RollingDistributionStream<Event> : RollingDistributionStreamBase
        where Event : IHystrixEvent
    {
        private readonly BehaviorSubject<CachedValuesHistogram> rollingDistribution = new BehaviorSubject<CachedValuesHistogram>(CachedValuesHistogram.BackedBy(CachedValuesHistogram.GetNewHistogram()));
        private readonly IObservable<CachedValuesHistogram> rollingDistributionStream;
        private AtomicReference<IDisposable> rollingDistributionSubscription = new AtomicReference<IDisposable>(null);

        protected RollingDistributionStream(IHystrixEventStream<Event> stream, int numBuckets, int bucketSizeInMs, Func<LongHistogram, Event, LongHistogram> addValuesToBucket)
        {
            List<LongHistogram> emptyDistributionsToStart = new List<LongHistogram>();
            for (int i = 0; i < numBuckets; i++)
            {
                emptyDistributionsToStart.Add(CachedValuesHistogram.GetNewHistogram());
            }

            Func<IObservable<Event>, IObservable<LongHistogram>> reduceBucketToSingleDistribution = (bucket) =>
            {
                var result = bucket.Aggregate(CachedValuesHistogram.GetNewHistogram(), (arg1, arg2) => addValuesToBucket(arg1, arg2)).Select(n => n);
                return result;
            };

            rollingDistributionStream = stream
                    .Observe()
                    .Window(TimeSpan.FromMilliseconds(bucketSizeInMs)) // stream of unaggregated buckets
                    .SelectMany((d) => reduceBucketToSingleDistribution(d)) // stream of aggregated Histograms
                    .StartWith(emptyDistributionsToStart) // stream of aggregated Histograms that starts with n empty
                    .Window(numBuckets, 1) // windowed stream: each OnNext is a stream of n Histograms
                    .SelectMany((w) => ReduceWindowToSingleDistribution(w)) // reduced stream: each OnNext is a single Histogram
                    .Map((h) => CacheHistogramValues(h)) // convert to CachedValueHistogram (commonly-accessed values are cached)
                    .Publish().RefCount();
        }

        public IObservable<CachedValuesHistogram> Observe()
        {
            return rollingDistributionStream;
        }

        public int LatestMean
        {
            get
            {
                CachedValuesHistogram latest = Latest;
                if (latest != null)
                {
                    return latest.GetMean();
                }
                else
                {
                    return 0;
                }
            }
        }

        public int GetLatestPercentile(double percentile)
        {
            CachedValuesHistogram latest = Latest;
            if (latest != null)
            {
                return latest.GetValueAtPercentile(percentile);
            }
            else
            {
                return 0;
            }
        }

        public void StartCachingStreamValuesIfUnstarted()
        {
            if (rollingDistributionSubscription.Value == null)
            {
                // the stream is not yet started
                IDisposable candidateSubscription = Observe().Subscribe(rollingDistribution);
                if (rollingDistributionSubscription.CompareAndSet(null, candidateSubscription))
                {
                    // won the race to set the subscription
                }
                else
                {
                    // lost the race to set the subscription, so we need to cancel this one
                    candidateSubscription.Dispose();
                }
            }
        }

        public CachedValuesHistogram Latest
        {
            get
            {
                StartCachingStreamValuesIfUnstarted();
                rollingDistribution.TryGetValue(out CachedValuesHistogram value);
                return value;
            }
        }

        public void Unsubscribe()
        {
            IDisposable s = rollingDistributionSubscription.Value;
            if (s != null)
            {
                s.Dispose();
                rollingDistributionSubscription.CompareAndSet(s, null);
            }
        }
    }
}
