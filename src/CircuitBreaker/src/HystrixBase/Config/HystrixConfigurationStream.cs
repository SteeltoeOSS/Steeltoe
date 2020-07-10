// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixConfigurationStream
    {
        private static int dataEmissionIntervalInMs = 5000;
        private readonly IObservable<HystrixConfiguration> _allConfigurationStream;
        private readonly AtomicBoolean _isSourceCurrentlySubscribed = new AtomicBoolean(false);

        private static Func<long, HystrixConfiguration> AllConfig { get; } =
            (long timestamp) =>
            {
                return HystrixConfiguration.From(
                        AllCommandConfig(timestamp),
                        AllThreadPoolConfig(timestamp),
                        AllCollapserConfig(timestamp));
            };

        public HystrixConfigurationStream(int intervalInMilliseconds)
        {
            this.IntervalInMilliseconds = intervalInMilliseconds;
            this._allConfigurationStream = Observable.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds))
                    .Map(AllConfig)
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
        private static readonly HystrixConfigurationStream INSTANCE =
                    new HystrixConfigurationStream(dataEmissionIntervalInMs);

        public static HystrixConfigurationStream GetInstance()
        {
            return INSTANCE;
        }

        // Return a ref-counted stream that will only do work when at least one subscriber is present
        public IObservable<HystrixConfiguration> Observe()
        {
            return _allConfigurationStream;
        }

        public IObservable<Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>> ObserveCommandConfiguration()
        {
            return _allConfigurationStream.Map(OnlyCommandConfig);
        }

        public IObservable<Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> ObserveThreadPoolConfiguration()
        {
            return _allConfigurationStream.Map(OnlyThreadPoolConfig);
        }

        public IObservable<Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> ObserveCollapserConfiguration()
        {
            return _allConfigurationStream.Map(OnlyCollapserConfig);
        }

        public int IntervalInMilliseconds { get; }

        public bool IsSourceCurrentlySubscribed
        {
            get { return _isSourceCurrentlySubscribed.Value; }
        }

        internal static HystrixConfigurationStream GetNonSingletonInstanceOnlyUsedInUnitTests(int delayInMs)
        {
            return new HystrixConfigurationStream(delayInMs);
        }

        private static HystrixCommandConfiguration SampleCommandConfiguration(
            IHystrixCommandKey commandKey,
            IHystrixThreadPoolKey threadPoolKey,
            IHystrixCommandGroupKey groupKey,
            IHystrixCommandOptions commandProperties)
        {
            return HystrixCommandConfiguration.Sample(commandKey, threadPoolKey, groupKey, commandProperties);
        }

        private static HystrixThreadPoolConfiguration SampleThreadPoolConfiguration(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions threadPoolProperties)
        {
            return HystrixThreadPoolConfiguration.Sample(threadPoolKey, threadPoolProperties);
        }

        private static HystrixCollapserConfiguration SampleCollapserConfiguration(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions collapserProperties)
        {
            return HystrixCollapserConfiguration.Sample(collapserKey, collapserProperties);
        }

        private static Func<long, Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>> AllCommandConfig { get; } =
            (long timestamp) =>
            {
                Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfigPerKey = new Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>();
                foreach (HystrixCommandMetrics commandMetrics in HystrixCommandMetrics.GetInstances())
                {
                    IHystrixCommandKey commandKey = commandMetrics.CommandKey;
                    IHystrixThreadPoolKey threadPoolKey = commandMetrics.ThreadPoolKey;
                    IHystrixCommandGroupKey groupKey = commandMetrics.CommandGroup;
                    commandConfigPerKey.Add(commandKey, SampleCommandConfiguration(commandKey, threadPoolKey, groupKey, commandMetrics.Properties));
                }

                return commandConfigPerKey;
            };

        private static Func<long, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> AllThreadPoolConfig { get; } =
            (long timestamp) =>
            {
                Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfigPerKey = new Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>();
                foreach (HystrixThreadPoolMetrics threadPoolMetrics in HystrixThreadPoolMetrics.GetInstances())
                {
                    IHystrixThreadPoolKey threadPoolKey = threadPoolMetrics.ThreadPoolKey;
                    threadPoolConfigPerKey.Add(threadPoolKey, SampleThreadPoolConfiguration(threadPoolKey, threadPoolMetrics.Properties));
                }

                return threadPoolConfigPerKey;
            };

        private static Func<long, Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> AllCollapserConfig { get; } =
            (long timestamp) =>
            {
                Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfigPerKey = new Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>();
                foreach (HystrixCollapserMetrics collapserMetrics in HystrixCollapserMetrics.GetInstances())
                {
                    IHystrixCollapserKey collapserKey = collapserMetrics.CollapserKey;
                    collapserConfigPerKey.Add(collapserKey, SampleCollapserConfiguration(collapserKey, collapserMetrics.Properties));
                }

                return collapserConfigPerKey;
            };

        private static Func<HystrixConfiguration, Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>> OnlyCommandConfig { get; } =
            (HystrixConfiguration hystrixConfiguration) =>
            {
                return hystrixConfiguration.CommandConfig;
            };

        private static Func<HystrixConfiguration, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> OnlyThreadPoolConfig { get; } =
            (HystrixConfiguration hystrixConfiguration) =>
            {
                return hystrixConfiguration.ThreadPoolConfig;
            };

        private static Func<HystrixConfiguration, Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> OnlyCollapserConfig { get; } =
            (HystrixConfiguration hystrixConfiguration) =>
            {
                return hystrixConfiguration.CollapserConfig;
            };
    }
}
