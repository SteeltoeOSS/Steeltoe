// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class BucketedCumulativeCounterStream<TEvent, TBucket, TOutput> : BucketedCounterStream<TEvent, TBucket, TOutput>
    where TEvent : IHystrixEvent
{
    private readonly AtomicBoolean _isSourceCurrentlySubscribed = new (false);
    private readonly BehaviorSubject<TOutput> _counterSubject;
    private readonly IObservable<TOutput> _sourceStream;

    protected BucketedCumulativeCounterStream(IHystrixEventStream<TEvent> stream, int numBuckets, int bucketSizeInMs, Func<TBucket, TEvent, TBucket> reduceCommandCompletion, Func<TOutput, TBucket, TOutput> reduceBucket)
        : base(stream, numBuckets, bucketSizeInMs, reduceCommandCompletion)
    {
        _counterSubject = new BehaviorSubject<TOutput>(EmptyOutputValue);
        _sourceStream = BucketedStream
            .Scan(EmptyOutputValue, reduceBucket)
            .Skip(numBuckets)
            .OnSubscribe(() => { _isSourceCurrentlySubscribed.Value = true; })
            .OnDispose(() => { _isSourceCurrentlySubscribed.Value = false; })
            .Publish().RefCount();           // multiple subscribers should get same data
    }

    public override IObservable<TOutput> Observe()
    {
        return _sourceStream;
    }

    public void StartCachingStreamValuesIfUnstarted()
    {
        if (Subscription.Value == null)
        {
            // the stream is not yet started
            var candidateSubscription = Observe().Subscribe(_counterSubject);
            if (Subscription.CompareAndSet(null, candidateSubscription))
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

    // Synchronous call to retrieve the last calculated bucket without waiting for any emissions
    // return last calculated bucket
    public TOutput Latest
    {
        get
        {
            if (_counterSubject.TryGetValue(out var v))
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
