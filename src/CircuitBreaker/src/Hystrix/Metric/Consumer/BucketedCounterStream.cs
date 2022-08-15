// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class BucketedCounterStream<TEvent, TBucket, TOutput>
    where TEvent : IHystrixEvent
{
    private readonly Func<IObservable<TEvent>, IObservable<TBucket>> _reduceBucketToSummary;
    protected readonly int NumBuckets;
    protected readonly int BucketSizeInMs;
    protected readonly IObservable<TBucket> BucketedStream;
    protected readonly AtomicReference<IDisposable> Subscription = new(null);

    public abstract TBucket EmptyBucketSummary { get; }

    public abstract TOutput EmptyOutputValue { get; }

    protected BucketedCounterStream(IHystrixEventStream<TEvent> inputEventStream, int numBuckets, int bucketSizeInMs,
        Func<TBucket, TEvent, TBucket> appendRawEventToBucket)
    {
        NumBuckets = numBuckets;
        BucketSizeInMs = bucketSizeInMs;

        _reduceBucketToSummary = eventsObservable =>
        {
            IObservable<TBucket> result = eventsObservable.Aggregate(EmptyBucketSummary, appendRawEventToBucket).Select(n => n);
            return result;
        };

        IList<TBucket> emptyEventCountsToStart = new List<TBucket>();

        for (int i = 0; i < numBuckets; i++)
        {
            emptyEventCountsToStart.Add(EmptyBucketSummary);
        }

        BucketedStream = Observable.Defer(() =>
        {
            return inputEventStream.Observe()
                .Window(TimeSpan.FromMilliseconds(bucketSizeInMs),
                    NewThreadScheduler.Default) // bucket it by the counter window so we can emit to the next operator in time chunks, not on every OnNext
                .SelectMany(b => _reduceBucketToSummary(b))
                .StartWith(emptyEventCountsToStart); // start it with empty arrays to make consumer logic as generic as possible (windows are always full)
        });
    }

    public abstract IObservable<TOutput> Observe();

    public void Unsubscribe()
    {
        IDisposable s = Subscription.Value;

        if (s != null)
        {
            s.Dispose();
            Subscription.CompareAndSet(s, null);
        }
    }
}
