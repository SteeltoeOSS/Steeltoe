// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class BucketedRollingCounterStream<TEvent, TBucket, TOutput> : BucketedCounterStream<TEvent, TBucket, TOutput>
    where TEvent : IHystrixEvent
{
    protected BehaviorSubject<TOutput> counterSubject;
    private readonly AtomicBoolean _isSourceCurrentlySubscribed = new (false);
    private readonly IObservable<TOutput> _sourceStream;

    protected BucketedRollingCounterStream(IHystrixEventStream<TEvent> stream, int numBuckets, int bucketSizeInMs, Func<TBucket, TEvent, TBucket> appendRawEventToBucket, Func<TOutput, TBucket, TOutput> reduceBucket)
        : base(stream, numBuckets, bucketSizeInMs, appendRawEventToBucket)
    {
        Func<IObservable<TBucket>, IObservable<TOutput>> reduceWindowToSummary = window =>
        {
            var result = window.Aggregate(EmptyOutputValue, reduceBucket).Select(n => n);
            return result;
        };
        counterSubject = new BehaviorSubject<TOutput>(EmptyOutputValue);
        _sourceStream = BucketedStream // stream broken up into buckets

            .Window(numBuckets, 1) // emit overlapping windows of buckets

            .FlatMap(w =>
                reduceWindowToSummary(w)) // convert a window of bucket-summaries into a single summary

            .OnSubscribe(() =>
            {
                _isSourceCurrentlySubscribed.Value = true;
            })
            .OnDispose(() =>
            {
                _isSourceCurrentlySubscribed.Value = false;
            })
            .Publish().RefCount();                // multiple subscribers should get same data
    }

    public override IObservable<TOutput> Observe()
    {
        return _sourceStream;
    }

    internal bool IsSourceCurrentlySubscribed
    {
        get
        {
            return _isSourceCurrentlySubscribed.Value;
        }
    }

    public void StartCachingStreamValuesIfUnstarted()
    {
        if (Subscription.Value == null)
        {
            // the stream is not yet started
            var candidateSubscription = Observe().Subscribe(counterSubject);
            if (!Subscription.CompareAndSet(null, candidateSubscription))
            {
                // lost the race to set the subscription, so we need to cancel this one
                candidateSubscription.Dispose();
            }
        }
    }

    // Synchronous call to retrieve the last calculated bucket without waiting for any emissions
    public TOutput Latest
    {
        get
        {
            if (counterSubject.TryGetValue(out var v))
            {
                return v;
            }
            else
            {
                return EmptyOutputValue;
            }
        }
    }
}
