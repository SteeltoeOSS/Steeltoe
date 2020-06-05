// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixUtilizationStream
    {
        private const int DataEmissionIntervalInMs = 500;
        private readonly int intervalInMilliseconds;
        private readonly IObservable<HystrixUtilization> allUtilizationStream;
        private readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);

        private static Func<long, HystrixUtilization> AllUtilization { get; } =
          (long timestamp) =>
        {
            return HystrixUtilization.From(
                    AllCommandUtilization(timestamp),
                    AllThreadPoolUtilization(timestamp));
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
        }

        // The data emission interval is looked up on startup only
        private static HystrixUtilizationStream instance =
                    new HystrixUtilizationStream(DataEmissionIntervalInMs);

        public static HystrixUtilizationStream GetInstance()
        {
            return instance;
        }

         // Return a ref-counted stream that will only do work when at least one subscriber is present
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

        internal static HystrixUtilizationStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixUtilizationStream(delayInMs);
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
