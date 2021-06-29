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
        private readonly IObservable<HystrixUtilization> _allUtilizationStream;
        private readonly AtomicBoolean _isSourceCurrentlySubscribed = new AtomicBoolean(false);

        private static Func<long, HystrixUtilization> AllUtilization { get; } =
          (long timestamp) =>
        {
            return HystrixUtilization.From(
                    AllCommandUtilization(timestamp),
                    AllThreadPoolUtilization(timestamp));
        };

        public HystrixUtilizationStream(int intervalInMilliseconds)
        {
            IntervalInMilliseconds = intervalInMilliseconds;
            _allUtilizationStream = Observable.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds))
                    .Map((t) => AllUtilization(t))
                    .OnSubscribe(() =>
                    {
                        _isSourceCurrentlySubscribed.Value = true;
                    })
                    .OnDispose(() =>
                    {
                        _isSourceCurrentlySubscribed.Value = false;
                    })
                    .Publish().RefCount();
        }

        // The data emission interval is looked up on startup only
        private static readonly HystrixUtilizationStream Instance =
                    new HystrixUtilizationStream(DataEmissionIntervalInMs);

        public static HystrixUtilizationStream GetInstance()
        {
            return Instance;
        }

        // Return a ref-counted stream that will only do work when at least one subscriber is present
        public IObservable<HystrixUtilization> Observe()
        {
            return _allUtilizationStream;
        }

        public IObservable<Dictionary<IHystrixCommandKey, HystrixCommandUtilization>> ObserveCommandUtilization()
        {
            return _allUtilizationStream.Map((a) => OnlyCommandUtilization(a));
        }

        public IObservable<Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>> ObserveThreadPoolUtilization()
        {
            return _allUtilizationStream.Map((a) => OnlyThreadPoolUtilization(a));
        }

        public int IntervalInMilliseconds { get; }

        public bool IsSourceCurrentlySubscribed
        {
            get { return _isSourceCurrentlySubscribed.Value; }
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
                var commandUtilizationPerKey = new Dictionary<IHystrixCommandKey, HystrixCommandUtilization>();
                foreach (var commandMetrics in HystrixCommandMetrics.GetInstances())
                {
                    var commandKey = commandMetrics.CommandKey;
                    commandUtilizationPerKey.Add(commandKey, SampleCommandUtilization(commandMetrics));
                }

                return commandUtilizationPerKey;
            };

        private static Func<long, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>> AllThreadPoolUtilization { get; } =
            (long timestamp) =>
            {
                var threadPoolUtilizationPerKey = new Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization>();
                foreach (var threadPoolMetrics in HystrixThreadPoolMetrics.GetInstances())
                {
                    var threadPoolKey = threadPoolMetrics.ThreadPoolKey;
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
