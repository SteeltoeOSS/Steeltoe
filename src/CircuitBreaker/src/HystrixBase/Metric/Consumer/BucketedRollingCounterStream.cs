// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class BucketedRollingCounterStream<Event, Bucket, Output> : BucketedCounterStream<Event, Bucket, Output>
        where Event : IHystrixEvent
    {
        protected BehaviorSubject<Output> counterSubject;
        private readonly AtomicBoolean _isSourceCurrentlySubscribed = new AtomicBoolean(false);
        private readonly IObservable<Output> _sourceStream;

        protected BucketedRollingCounterStream(IHystrixEventStream<Event> stream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> appendRawEventToBucket, Func<Output, Bucket, Output> reduceBucket)
            : base(stream, numBuckets, bucketSizeInMs, appendRawEventToBucket)
        {
            Func<IObservable<Bucket>, IObservable<Output>> reduceWindowToSummary = (window) =>
            {
                var result = window.Aggregate(EmptyOutputValue, (arg1, arg2) => reduceBucket(arg1, arg2)).Select(n => n);
                return result;
            };
            counterSubject = new BehaviorSubject<Output>(EmptyOutputValue);
            _sourceStream = bucketedStream // stream broken up into buckets

                .Window(numBuckets, 1) // emit overlapping windows of buckets

                .FlatMap((w) =>
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

        public override IObservable<Output> Observe()
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
            if (subscription.Value == null)
            {
                // the stream is not yet started
                var candidateSubscription = Observe().Subscribe(counterSubject);
                if (subscription.CompareAndSet(null, candidateSubscription))
                {
                }
                else
                {
                    // lost the race to set the subscription, so we need to cancel this one
                    candidateSubscription.Dispose();
                }
            }
        }

        // Synchronous call to retrieve the last calculated bucket without waiting for any emissions
        public Output Latest
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
}
