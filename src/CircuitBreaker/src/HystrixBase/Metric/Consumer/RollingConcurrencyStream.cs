// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Reactive.Subjects;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public abstract class RollingConcurrencyStream
{
    private readonly BehaviorSubject<int> _rollingMax = new (0);
    private readonly IObservable<int> _rollingMaxStream;
    private readonly AtomicReference<IDisposable> _rollingMaxSubscription = new (null);

    private static Func<int, int, int> ReduceToMax { get; } = Math.Max;

    private static Func<IObservable<int>, IObservable<int>> ReduceStreamToMax { get; } = observedConcurrency =>
    {
        return observedConcurrency.Aggregate(0, (arg1, arg2) => ReduceToMax(arg1, arg2)).Select(n => n);
    };

    private static Func<HystrixCommandExecutionStarted, int> GetConcurrencyCountFromEvent { get; } = @event => @event.CurrentConcurrency;

    protected RollingConcurrencyStream(IHystrixEventStream<HystrixCommandExecutionStarted> inputEventStream, int numBuckets, int bucketSizeInMs)
    {
        var emptyRollingMaxBuckets = new List<int>();
        for (var i = 0; i < numBuckets; i++)
        {
            emptyRollingMaxBuckets.Add(0);
        }

        _rollingMaxStream = inputEventStream
            .Observe()
            .Map(arg => GetConcurrencyCountFromEvent(arg))
            .Window(TimeSpan.FromMilliseconds(bucketSizeInMs), NewThreadScheduler.Default)
            .SelectMany(arg => ReduceStreamToMax(arg))
            .StartWith(emptyRollingMaxBuckets)
            .Window(numBuckets, 1)
            .SelectMany(arg => ReduceStreamToMax(arg))
            .Publish().RefCount();
    }

    public void StartCachingStreamValuesIfUnstarted()
    {
        if (_rollingMaxSubscription.Value == null)
        {
            // the stream is not yet started
            var candidateSubscription = Observe().Subscribe(_rollingMax);
            if (_rollingMaxSubscription.CompareAndSet(null, candidateSubscription))
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
            _rollingMax.TryGetValue(out var value);
            return value;
        }
    }

    public IObservable<int> Observe()
    {
        return _rollingMaxStream;
    }

    public void Unsubscribe()
    {
        var s = _rollingMaxSubscription.Value;
        if (s != null)
        {
            s.Dispose();
            _rollingMaxSubscription.CompareAndSet(s, null);
        }
    }
}
