// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class BucketedCounterStream<Event, Bucket, Output>
    where Event : IHystrixEvent
{
    protected readonly int numBuckets;
    protected readonly int bucketSizeInMs;
    protected readonly IObservable<Bucket> bucketedStream;
    protected readonly AtomicReference<IDisposable> subscription = new (null);

    private readonly Func<IObservable<Event>, IObservable<Bucket>> _reduceBucketToSummary;

    protected BucketedCounterStream(IHystrixEventStream<Event> inputEventStream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> appendRawEventToBucket)
    {
        this.numBuckets = numBuckets;
        this.bucketSizeInMs = bucketSizeInMs;
        _reduceBucketToSummary = eventsObservable =>
        {
            var result = eventsObservable.Aggregate(EmptyBucketSummary, appendRawEventToBucket).Select(n => n);
            return result;
        };

        IList<Bucket> emptyEventCountsToStart = new List<Bucket>();
        for (var i = 0; i < numBuckets; i++)
        {
            emptyEventCountsToStart.Add(EmptyBucketSummary);
        }

        bucketedStream = Observable.Defer(() =>
        {
            return inputEventStream
                .Observe()
                .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default) // bucket it by the counter window so we can emit to the next operator in time chunks, not on every OnNext
                .SelectMany(b => _reduceBucketToSummary(b))
                .StartWith(emptyEventCountsToStart);           // start it with empty arrays to make consumer logic as generic as possible (windows are always full)
        });
    }

    public abstract Bucket EmptyBucketSummary { get; }

    public abstract Output EmptyOutputValue { get; }

    public abstract IObservable<Output> Observe();

    public void Unsubscribe()
    {
        var s = subscription.Value;
        if (s != null)
        {
            s.Dispose();
            subscription.CompareAndSet(s, null);
        }
    }
}
