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
        private readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);
        private readonly IObservable<Output> sourceStream;

        protected BucketedRollingCounterStream(IHystrixEventStream<Event> stream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> appendRawEventToBucket, Func<Output, Bucket, Output> reduceBucket)
            : base(stream, numBuckets, bucketSizeInMs, appendRawEventToBucket)
        {
            Func<IObservable<Bucket>, IObservable<Output>> reduceWindowToSummary = (window) =>
            {
                var result = window.Aggregate(EmptyOutputValue, (arg1, arg2) => reduceBucket(arg1, arg2)).Select(n => n);
                return result;
            };
            counterSubject = new BehaviorSubject<Output>(EmptyOutputValue);
            sourceStream = bucketedStream // stream broken up into buckets

                .Window(numBuckets, 1) // emit overlapping windows of buckets

                .FlatMap((w) =>
                    reduceWindowToSummary(w)) // convert a window of bucket-summaries into a single summary

                .OnSubscribe(() =>
                {
                    isSourceCurrentlySubscribed.Value = true;
                })
                .OnDispose(() =>
                 {
                     isSourceCurrentlySubscribed.Value = false;
                 })
                .Publish().RefCount();                // multiple subscribers should get same data
        }

        public override IObservable<Output> Observe()
        {
            return sourceStream;
        }

        internal bool IsSourceCurrentlySubscribed
        {
            get
            {
                return isSourceCurrentlySubscribed.Value;
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
