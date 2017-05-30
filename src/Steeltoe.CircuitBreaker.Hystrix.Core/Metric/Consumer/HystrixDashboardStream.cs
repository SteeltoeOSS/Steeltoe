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
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer
{
    public class HystrixDashboardStream
    {
        private const int Default_Dashboard_IntervalInMilliseconds = 500;
        readonly int delayInMs;
        readonly IObservable<DashboardData> singleSource;
        readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);

        //private static final DynamicIntProperty dataEmissionIntervalInMs =
        //    DynamicPropertyFactory.getInstance().getIntProperty("hystrix.stream.dashboard.intervalInMilliseconds", 500);
        // TODO: 

        private HystrixDashboardStream(int delayInMs)
        {
            this.delayInMs = delayInMs;
            this.singleSource = Observable.Interval(TimeSpan.FromMilliseconds(delayInMs))
                                .Map((timestamp) => { return new DashboardData(HystrixCommandMetrics.GetInstances(), HystrixThreadPoolMetrics.GetInstances(), HystrixCollapserMetrics.GetInstances()); })
                                .OnSubscribe(() => { isSourceCurrentlySubscribed.Value = true; })
                                .OnDispose(() => { isSourceCurrentlySubscribed.Value = false; })
                                .Publish().RefCount();
            //.onBackpressureDrop();
        }

        //The data emission interval is looked up on startup only
        private static HystrixDashboardStream INSTANCE =
                new HystrixDashboardStream(Default_Dashboard_IntervalInMilliseconds);

        public static HystrixDashboardStream GetInstance()
        {
            return INSTANCE;
        }

        internal static HystrixDashboardStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixDashboardStream(delayInMs);
        }

        /**
         * Return a ref-counted stream that will only do work when at least one subscriber is present
         */
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

        public class DashboardData
        {
            internal readonly ICollection<HystrixCommandMetrics> commandMetrics;
            internal readonly ICollection<HystrixThreadPoolMetrics> threadPoolMetrics;
            internal readonly ICollection<HystrixCollapserMetrics> collapserMetrics;

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
