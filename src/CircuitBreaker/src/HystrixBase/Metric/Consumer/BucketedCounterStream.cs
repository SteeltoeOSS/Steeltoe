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

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class BucketedCounterStream<Event, Bucket, Output>
        where Event : IHystrixEvent
    {
        protected readonly int numBuckets;
        protected readonly int bucketSizeInMs;
        protected readonly IObservable<Bucket> bucketedStream;
        protected readonly AtomicReference<IDisposable> subscription = new AtomicReference<IDisposable>(null);

        private readonly Func<IObservable<Event>, IObservable<Bucket>> reduceBucketToSummary;

        protected BucketedCounterStream(IHystrixEventStream<Event> inputEventStream, int numBuckets, int bucketSizeInMs, Func<Bucket, Event, Bucket> appendRawEventToBucket)
        {
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
                    .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default) // bucket it by the counter window so we can emit to the next operator in time chunks, not on every OnNext
                    .SelectMany((b) =>
                    {
                        return reduceBucketToSummary(b);
                    })
                    .StartWith(emptyEventCountsToStart);           // start it with empty arrays to make consumer logic as generic as possible (windows are always full)
            });
        }

        public abstract Bucket EmptyBucketSummary { get; }

        public abstract Output EmptyOutputValue { get; }

        public abstract IObservable<Output> Observe();

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
