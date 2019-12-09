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

using Steeltoe.CircuitBreaker.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public abstract class RollingConcurrencyStream
    {
        private readonly BehaviorSubject<int> rollingMax = new BehaviorSubject<int>(0);
        private readonly IObservable<int> rollingMaxStream;
        private AtomicReference<IDisposable> rollingMaxSubscription = new AtomicReference<IDisposable>(null);

        private static Func<int, int, int> ReduceToMax { get; } = (a, b) =>
       {
           return Math.Max(a, b);
       };

        private static Func<IObservable<int>, IObservable<int>> ReduceStreamToMax { get; } = (observedConcurrency) =>
        {
            return observedConcurrency.Aggregate(0, (arg1, arg2) => ReduceToMax(arg1, arg2)).Select(n => n);
        };

        private static Func<HystrixCommandExecutionStarted, int> GetConcurrencyCountFromEvent { get; } = (@event) =>
        {
            return @event.CurrentConcurrency;
        };

        protected RollingConcurrencyStream(IHystrixEventStream<HystrixCommandExecutionStarted> inputEventStream, int numBuckets, int bucketSizeInMs)
        {
            List<int> emptyRollingMaxBuckets = new List<int>();
            for (int i = 0; i < numBuckets; i++)
            {
                emptyRollingMaxBuckets.Add(0);
            }

            rollingMaxStream = inputEventStream
                    .Observe()
                    .Map((arg) => GetConcurrencyCountFromEvent(arg))
                    .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default)
                    .SelectMany((arg) => ReduceStreamToMax(arg))
                    .StartWith(emptyRollingMaxBuckets)
                    .Window(numBuckets, 1)
                    .SelectMany((arg) => ReduceStreamToMax(arg))
                    .Publish().RefCount();
        }

        public void StartCachingStreamValuesIfUnstarted()
        {
            if (rollingMaxSubscription.Value == null)
            {
                // the stream is not yet started
                IDisposable candidateSubscription = Observe().Subscribe(rollingMax);
                if (rollingMaxSubscription.CompareAndSet(null, candidateSubscription))
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

        public long LatestRollingMax
        {
            get
            {
                rollingMax.TryGetValue(out int value);
                return value;
            }
        }

        public IObservable<int> Observe()
        {
            return rollingMaxStream;
        }

        public void Unsubscribe()
        {
            IDisposable s = rollingMaxSubscription.Value;
            if (s != null)
            {
                s.Dispose();
                rollingMaxSubscription.CompareAndSet(s, null);
            }
        }
    }
}
