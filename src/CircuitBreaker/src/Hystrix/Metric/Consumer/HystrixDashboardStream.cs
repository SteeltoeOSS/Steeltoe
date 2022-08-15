// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;

public class HystrixDashboardStream
{
    private const int DefaultDashboardIntervalInMilliseconds = 500;

    // The data emission interval is looked up on startup only
    private static readonly HystrixDashboardStream Instance = new(DefaultDashboardIntervalInMilliseconds);

    private readonly int _delayInMs;
    private readonly IObservable<DashboardData> _singleSource;
    private readonly AtomicBoolean _isSourceCurrentlySubscribed = new(false);

    public bool IsSourceCurrentlySubscribed => _isSourceCurrentlySubscribed.Value;

    private HystrixDashboardStream(int delayInMs)
    {
        _delayInMs = delayInMs;

        _singleSource = Observable.Interval(TimeSpan.FromMilliseconds(delayInMs)).Map(_ =>
                new DashboardData(HystrixCommandMetrics.GetInstances(), HystrixThreadPoolMetrics.GetInstances(), HystrixCollapserMetrics.GetInstances()))
            .OnSubscribe(() =>
            {
                _isSourceCurrentlySubscribed.Value = true;
            }).OnDispose(() =>
            {
                _isSourceCurrentlySubscribed.Value = false;
            }).Publish().RefCount();
    }

    public static HystrixDashboardStream GetInstance()
    {
        return Instance;
    }

    // Return a ref-counted stream that will only do work when at least one subscriber is present
    public IObservable<DashboardData> Observe()
    {
        return _singleSource;
    }

    internal static HystrixDashboardStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
    {
        return new HystrixDashboardStream(delayInMs);
    }

    public class DashboardData
    {
        public ICollection<HystrixCommandMetrics> CommandMetrics { get; }

        public ICollection<HystrixThreadPoolMetrics> ThreadPoolMetrics { get; }

        public ICollection<HystrixCollapserMetrics> CollapserMetrics { get; }

        public DashboardData(ICollection<HystrixCommandMetrics> commandMetrics, ICollection<HystrixThreadPoolMetrics> threadPoolMetrics,
            ICollection<HystrixCollapserMetrics> collapserMetrics)
        {
            CommandMetrics = commandMetrics;
            ThreadPoolMetrics = threadPoolMetrics;
            CollapserMetrics = collapserMetrics;
        }
    }
}
