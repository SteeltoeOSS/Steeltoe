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


namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixUtilizationStream
    {
        private readonly int intervalInMilliseconds;
        private readonly IObservable<HystrixUtilization> allUtilizationStream;
        private readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);
        private static readonly int dataEmissionIntervalInMs = 500;

        //private static final DynamicIntProperty dataEmissionIntervalInMs =
        //    DynamicPropertyFactory.getInstance().getIntProperty("hystrix.stream.utilization.intervalInMilliseconds", 500);


        private static Func<long, HystrixUtilization> AllUtilization { get; } =
          (long timestamp) =>
        {
            return HystrixUtilization.From(
                    AllCommandUtilization(timestamp),
                    AllThreadPoolUtilization(timestamp)
            );
        };


        public HystrixUtilizationStream(int intervalInMilliseconds)
        {
            this.intervalInMilliseconds = intervalInMilliseconds;
            this.allUtilizationStream = Observable.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds))
                    .Map((t) => AllUtilization(t))
                    .OnSubscribe(() =>
                    {
                        isSourceCurrentlySubscribed.Value = true;

                    })
                    .OnDispose(() =>
                    {
                        isSourceCurrentlySubscribed.Value = false;

                    })
                    .Publish().RefCount();
            //.onBackpressureDrop();
        }

        //The data emission interval is looked up on startup only
        private static HystrixUtilizationStream INSTANCE =
                    new HystrixUtilizationStream(dataEmissionIntervalInMs);

        public static HystrixUtilizationStream GetInstance()
        {
            return INSTANCE;
        }

        internal static HystrixUtilizationStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixUtilizationStream(delayInMs);
        }

        /**
         * Return a ref-counted stream that will only do work when at least one subscriber is present
         */
        public IObservable<HystrixUtilization> Observe()
        {
            return allUtilizationStream;
        }

        public IObservable<Dictionary<IHystrixCommandKey, HystrixCommandUtilization>> ObserveCommandUtilization()
        {
            return allUtilizationStream.Map((a) => OnlyCommandUtilization(a));
        }

        public IObservable<Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>> ObserveThreadPoolUtilization()
        {
            return allUtilizationStream.Map((a) => OnlyThreadPoolUtilization(a));
        }

        public int IntervalInMilliseconds
        {
            get { return this.intervalInMilliseconds; }
        }

        public bool IsSourceCurrentlySubscribed
        {
            get { return isSourceCurrentlySubscribed.Value; }
        }

        private static HystrixCommandUtilization SampleCommandUtilization(HystrixCommandMetrics commandMetrics)
        {
            return HystrixCommandUtilization.Sample(commandMetrics);
        }

        private static HystrixThreadPoolUtilization SampleThreadPoolUtilization(HystrixThreadPoolMetrics threadPoolMetrics)
        {
            return HystrixThreadPoolUtilization.Sample(threadPoolMetrics);
        }

        private static Func<long, Dictionary<IHystrixCommandKey, HystrixCommandUtilization>> AllCommandUtilization { get; } =
            (long timestamp) =>
            {
                Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationPerKey = new Dictionary<IHystrixCommandKey, HystrixCommandUtilization>();
                foreach (HystrixCommandMetrics commandMetrics in HystrixCommandMetrics.GetInstances())
                {
                    IHystrixCommandKey commandKey = commandMetrics.CommandKey;
                    commandUtilizationPerKey.Add(commandKey, SampleCommandUtilization(commandMetrics));
                }
                return commandUtilizationPerKey;

            };

        private static Func<long, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>> AllThreadPoolUtilization { get; } =
            (long timestamp) =>
            {
                Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationPerKey = new Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>();
                foreach (HystrixThreadPoolMetrics threadPoolMetrics in HystrixThreadPoolMetrics.GetInstances())
                {
                    IHystrixThreadPoolKey threadPoolKey = threadPoolMetrics.ThreadPoolKey;
                    threadPoolUtilizationPerKey.Add(threadPoolKey, SampleThreadPoolUtilization(threadPoolMetrics));
                }
                return threadPoolUtilizationPerKey;

            };

        private static Func<HystrixUtilization, Dictionary<IHystrixCommandKey, HystrixCommandUtilization>> OnlyCommandUtilization { get; } =
            (HystrixUtilization hystrixUtilization) =>
            {
                return hystrixUtilization.CommandUtilizationMap;
            };

        private static Func<HystrixUtilization, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>> OnlyThreadPoolUtilization { get; } =
            (HystrixUtilization hystrixUtilization) =>
            {
                return hystrixUtilization.ThreadPoolUtilizationMap;
            };
    }

}
