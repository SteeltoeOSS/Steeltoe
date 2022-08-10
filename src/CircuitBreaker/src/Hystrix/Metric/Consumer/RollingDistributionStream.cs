// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;
using HdrHistogram;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class RollingDistributionStream<TEvent> : RollingDistributionStreamBase
    where TEvent : IHystrixEvent
{
    private readonly BehaviorSubject<CachedValuesHistogram> _rollingDistribution = new(CachedValuesHistogram.BackedBy(CachedValuesHistogram.GetNewHistogram()));
    private readonly IObservable<CachedValuesHistogram> _rollingDistributionStream;
    private readonly AtomicReference<IDisposable> _rollingDistributionSubscription = new(null);

    public int LatestMean
    {
        get
        {
            CachedValuesHistogram latest = Latest;

            if (latest != null)
            {
                return latest.GetMean();
            }

            return 0;
        }
    }

    public CachedValuesHistogram Latest
    {
        get
        {
            _rollingDistribution.TryGetValue(out CachedValuesHistogram value);
            return value;
        }
    }

    protected RollingDistributionStream(IHystrixEventStream<TEvent> stream, int numBuckets, int bucketSizeInMs,
        Func<LongHistogram, TEvent, LongHistogram> addValuesToBucket)
    {
        var emptyDistributionsToStart = new List<LongHistogram>();

        for (int i = 0; i < numBuckets; i++)
        {
            emptyDistributionsToStart.Add(CachedValuesHistogram.GetNewHistogram());
        }

        Func<IObservable<TEvent>, IObservable<LongHistogram>> reduceBucketToSingleDistribution = bucket =>
        {
            IObservable<LongHistogram> result = bucket.Aggregate(CachedValuesHistogram.GetNewHistogram(), addValuesToBucket).Select(n => n);
            return result;
        };

        _rollingDistributionStream = stream.Observe()
            .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default) // stream of unaggregated buckets
            .SelectMany(d => reduceBucketToSingleDistribution(d)) // stream of aggregated Histograms
            .StartWith(emptyDistributionsToStart) // stream of aggregated Histograms that starts with n empty
            .Window(numBuckets, 1) // windowed stream: each OnNext is a stream of n Histograms
            .SelectMany(w => ReduceWindowToSingleDistribution(w)) // reduced stream: each OnNext is a single Histogram
            .Map(h => CacheHistogramValues(h)) // convert to CachedValueHistogram (commonly-accessed values are cached)
            .Publish().RefCount();
    }

    public IObservable<CachedValuesHistogram> Observe()
    {
        return _rollingDistributionStream;
    }

    public int GetLatestPercentile(double percentile)
    {
        CachedValuesHistogram latest = Latest;

        if (latest != null)
        {
            return latest.GetValueAtPercentile(percentile);
        }

        return 0;
    }

    public void StartCachingStreamValuesIfUnstarted()
    {
        if (_rollingDistributionSubscription.Value == null)
        {
            // the stream is not yet started
            IDisposable candidateSubscription = Observe().Subscribe(_rollingDistribution);

            if (_rollingDistributionSubscription.CompareAndSet(null, candidateSubscription))
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

    public void Unsubscribe()
    {
        IDisposable s = _rollingDistributionSubscription.Value;

        if (s != null)
        {
            s.Dispose();
            _rollingDistributionSubscription.CompareAndSet(s, null);
        }
    }
}
