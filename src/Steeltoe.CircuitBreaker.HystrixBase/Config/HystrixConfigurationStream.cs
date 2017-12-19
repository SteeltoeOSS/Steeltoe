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

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixConfigurationStream
    {
        private static int dataEmissionIntervalInMs = 5000;
        private readonly int intervalInMilliseconds;
        private readonly IObservable<HystrixConfiguration> allConfigurationStream;
        private readonly AtomicBoolean isSourceCurrentlySubscribed = new AtomicBoolean(false);

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
            this.intervalInMilliseconds = intervalInMilliseconds;
            this.allConfigurationStream = Observable.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds))
                    .Map(AllConfig)
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
        private static readonly HystrixConfigurationStream INSTANCE =
                    new HystrixConfigurationStream(dataEmissionIntervalInMs);

        public static HystrixConfigurationStream GetInstance()
        {
            return INSTANCE;
        }

         // Return a ref-counted stream that will only do work when at least one subscriber is present
        public IObservable<HystrixConfiguration> Observe()
        {
            return allConfigurationStream;
        }

        public IObservable<Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>> ObserveCommandConfiguration()
        {
            return allConfigurationStream.Map(OnlyCommandConfig);
        }

        public IObservable<Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> ObserveThreadPoolConfiguration()
        {
            return allConfigurationStream.Map(OnlyThreadPoolConfig);
        }

        public IObservable<Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> ObserveCollapserConfiguration()
        {
            return allConfigurationStream.Map(OnlyCollapserConfig);
        }

        public int IntervalInMilliseconds
        {
            get { return this.intervalInMilliseconds; }
        }

        public bool IsSourceCurrentlySubscribed
        {
            get { return isSourceCurrentlySubscribed.Value; }
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
