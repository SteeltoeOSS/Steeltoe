// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using HdrHistogram;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
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
                    .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default) // stream of unaggregated buckets
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
