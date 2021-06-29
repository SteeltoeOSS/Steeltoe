﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class BucketedCumulativeCounterStream<Event, Bucket, Output> : BucketedCounterStream<Event, Bucket, Output>
        where Event : IHystrixEvent
    {
        private readonly AtomicBoolean _isSourceCurrentlySubscribed = new AtomicBoolean(false);
        private readonly BehaviorSubject<Output> _counterSubject;
        private readonly IObservable<Output> _sourceStream;

        protected BucketedCumulativeCounterStream(IHystrixEventStream<Event> stream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> reduceCommandCompletion, Func<Output, Bucket, Output> reduceBucket)
            : base(stream, numBuckets, bucketSizeInMs, reduceCommandCompletion)
        {
            _counterSubject = new BehaviorSubject<Output>(EmptyOutputValue);
            _sourceStream = bucketedStream
                    .Scan(EmptyOutputValue, (arg1, arg2) => reduceBucket(arg1, arg2))
                    .Skip(numBuckets)
                    .OnSubscribe(() => { _isSourceCurrentlySubscribed.Value = true; })
                    .OnDispose(() => { _isSourceCurrentlySubscribed.Value = false; })
                    .Publish().RefCount();           // multiple subscribers should get same data
        }

        public override IObservable<Output> Observe()
        {
            return _sourceStream;
        }

        public void StartCachingStreamValuesIfUnstarted()
        {
            if (subscription.Value == null)
            {
                // the stream is not yet started
                var candidateSubscription = Observe().Subscribe(_counterSubject);
                if (subscription.CompareAndSet(null, candidateSubscription))
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
        public Output Latest
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
}
