// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class HystrixDashboardStream
    {
        private const int Default_Dashboard_IntervalInMilliseconds = 500;
        private readonly int delayInMs;
        private readonly IObservable<DashboardData> singleSource;
        private readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);

        private HystrixDashboardStream(int delayInMs)
        {
            this.delayInMs = delayInMs;
            this.singleSource = Observable.Interval(TimeSpan.FromMilliseconds(delayInMs))
                                .Map((timestamp) => { return new DashboardData(HystrixCommandMetrics.GetInstances(), HystrixThreadPoolMetrics.GetInstances(), HystrixCollapserMetrics.GetInstances()); })
                                .OnSubscribe(() => { isSourceCurrentlySubscribed.Value = true; })
                                .OnDispose(() => { isSourceCurrentlySubscribed.Value = false; })
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
            return singleSource;
        }

        public bool IsSourceCurrentlySubscribed
        {
            get
            {
                return isSourceCurrentlySubscribed.Value;
            }
        }

        internal static HystrixDashboardStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixDashboardStream(delayInMs);
        }

        public class DashboardData
        {
            private readonly ICollection<HystrixCommandMetrics> commandMetrics;
            private readonly ICollection<HystrixThreadPoolMetrics> threadPoolMetrics;
            private readonly ICollection<HystrixCollapserMetrics> collapserMetrics;

            public DashboardData(ICollection<HystrixCommandMetrics> commandMetrics, ICollection<HystrixThreadPoolMetrics> threadPoolMetrics, ICollection<HystrixCollapserMetrics> collapserMetrics)
            {
                this.commandMetrics = commandMetrics;
                this.threadPoolMetrics = threadPoolMetrics;
                this.collapserMetrics = collapserMetrics;
            }

            public ICollection<HystrixCommandMetrics> CommandMetrics
            {
                get
                {
                    return commandMetrics;
                }
            }

            public ICollection<HystrixThreadPoolMetrics> ThreadPoolMetrics
            {
                get
                {
                    return threadPoolMetrics;
                }
            }

            public ICollection<HystrixCollapserMetrics> CollapserMetrics
            {
                get
                {
                    return collapserMetrics;
                }
            }
        }
    }
}
