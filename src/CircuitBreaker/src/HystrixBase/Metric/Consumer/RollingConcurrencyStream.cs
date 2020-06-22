﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
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
