// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class HystrixDashboardStream
    {
        private const int Default_Dashboard_IntervalInMilliseconds = 500;
        private readonly int _delayInMs;
        private readonly IObservable<DashboardData> _singleSource;
        private readonly AtomicBoolean _isSourceCurrentlySubscribed = new AtomicBoolean(false);

        private HystrixDashboardStream(int delayInMs)
        {
            _delayInMs = delayInMs;
            _singleSource = Observable.Interval(TimeSpan.FromMilliseconds(delayInMs))
                                .Map((timestamp) => { return new DashboardData(HystrixCommandMetrics.GetInstances(), HystrixThreadPoolMetrics.GetInstances(), HystrixCollapserMetrics.GetInstances()); })
                                .OnSubscribe(() => { _isSourceCurrentlySubscribed.Value = true; })
                                .OnDispose(() => { _isSourceCurrentlySubscribed.Value = false; })
                                .Publish().RefCount();
        }

        // The data emission interval is looked up on startup only
        private static HystrixDashboardStream instance =
                new HystrixDashboardStream(Default_Dashboard_IntervalInMilliseconds);

        public static HystrixDashboardStream GetInstance()
        {
            return instance;
        }

         // Return a ref-counted stream that will only do work when at least one subscriber is present
        public IObservable<DashboardData> Observe()
        {
            return _singleSource;
        }

        public bool IsSourceCurrentlySubscribed
        {
            get
            {
                return _isSourceCurrentlySubscribed.Value;
            }
        }

        internal static HystrixDashboardStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixDashboardStream(delayInMs);
        }

        public class DashboardData
        {
            public DashboardData(ICollection<HystrixCommandMetrics> commandMetrics, ICollection<HystrixThreadPoolMetrics> threadPoolMetrics, ICollection<HystrixCollapserMetrics> collapserMetrics)
            {
                CommandMetrics = commandMetrics;
                ThreadPoolMetrics = threadPoolMetrics;
                CollapserMetrics = collapserMetrics;
            }

            public ICollection<HystrixCommandMetrics> CommandMetrics { get; }

            public ICollection<HystrixThreadPoolMetrics> ThreadPoolMetrics { get; }

            public ICollection<HystrixCollapserMetrics> CollapserMetrics { get; }
        }
    }
}
