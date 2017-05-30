//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class BucketedCounterStream<Event, Bucket, Output> where Event : IHystrixEvent
    {
        // TODO: protected
        public readonly int numBuckets;
        public readonly int bucketSizeInMs;
        protected readonly IObservable<Bucket> bucketedStream;
        protected readonly AtomicReference<IDisposable> subscription = new AtomicReference<IDisposable>(null);

        private readonly Func<IObservable<Event>, IObservable<Bucket>> reduceBucketToSummary;
        //protected readonly BehaviorSubject<Output> counterSubject;
        protected BucketedCounterStream(IHystrixEventStream<Event> inputEventStream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> appendRawEventToBucket)
        {
            //this.counterSubject = new BehaviorSubject<Output>(EmptyOutputValue);
            this.numBuckets = numBuckets;
            this.bucketSizeInMs = bucketSizeInMs;
            this.reduceBucketToSummary = (eventsObservable) =>
            {
                var result = eventsObservable.Aggregate(EmptyBucketSummary, (arg1, arg2) => appendRawEventToBucket(arg1, arg2)).Select(n => n);
                return result;
            };


            IList<Bucket> emptyEventCountsToStart = new List<Bucket>();
            for (int i = 0; i < numBuckets; i++)
            {
                emptyEventCountsToStart.Add(EmptyBucketSummary);
            }
            this.bucketedStream = Observable.Defer(() =>
            {
                return inputEventStream
                    .Observe()
                    .Window(TimeSpan.FromMilliseconds(bucketSizeInMs)) //bucket it by the counter window so we can emit to the next operator in time chunks, not on every OnNext
                    .SelectMany((b) =>
                    {
                        return reduceBucketToSummary(b);
                    })
                    .StartWith(emptyEventCountsToStart);           //start it with empty arrays to make consumer logic as generic as possible (windows are always full)
            });

        }

        public abstract Bucket EmptyBucketSummary { get; }

        public abstract Output EmptyOutputValue { get; }


        public abstract IObservable<Output> Observe();

        //public void StartCachingStreamValuesIfUnstarted()
        //{
        //    if (subscription.Value == null)
        //    {
        //        //the stream is not yet started
        //        IDisposable candidateSubscription = Observe().Subscribe(counterSubject);
        //        if (subscription.CompareAndSet(null, candidateSubscription))
        //        {
        //            //won the race to set the subscription
        //        }
        //        else
        //        {
        //            //lost the race to set the subscription, so we need to cancel this one
        //            candidateSubscription.Dispose();
        //        }
        //    }
        //}

        /**
         * Synchronous call to retrieve the last calculated bucket without waiting for any emissions
         * @return last calculated bucket
         */
        //public Output Latest
        //{
        //    get
        //    {
        //        StartCachingStreamValuesIfUnstarted();
        //        Output v = default(Output);
        //        if (counterSubject.TryGetValue(out v))
        //        {
        //            return v;
        //        }
        //        else
        //        {
        //            return EmptyOutputValue;
        //        }
        //    }
        //}

        public void Unsubscribe()
        {
            IDisposable s = subscription.Value;
            if (s != null)
            {
                s.Dispose();
                subscription.CompareAndSet(s, null);
            }
        }
    }
}
