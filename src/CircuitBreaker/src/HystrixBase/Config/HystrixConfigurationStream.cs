// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Config;

public class HystrixConfigurationStream
{
    private const int DataEmissionIntervalInMs = 5000;

    // The data emission interval is looked up on startup only
    private static readonly Lazy<HystrixConfigurationStream> LazyInstance = new(() => new(DataEmissionIntervalInMs),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly IObservable<HystrixConfiguration> _allConfigurationStream;
    private readonly AtomicBoolean _isSourceCurrentlySubscribed = new (false);

    private static Func<long, HystrixConfiguration> AllConfig { get; } =
        timestamp => HystrixConfiguration.From(
            AllCommandConfig(timestamp),
            AllThreadPoolConfig(timestamp),
            AllCollapserConfig(timestamp));

    public HystrixConfigurationStream(int intervalInMilliseconds)
    {
        IntervalInMilliseconds = intervalInMilliseconds;
        _allConfigurationStream = Observable.Interval(TimeSpan.FromMilliseconds(intervalInMilliseconds))
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

    public static HystrixConfigurationStream GetInstance()
    {
        return LazyInstance.Value;
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
        _ =>
        {
            var commandConfigPerKey = new Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>();
            foreach (var commandMetrics in HystrixCommandMetrics.GetInstances())
            {
                var commandKey = commandMetrics.CommandKey;
                var threadPoolKey = commandMetrics.ThreadPoolKey;
                var groupKey = commandMetrics.CommandGroup;
                commandConfigPerKey.Add(commandKey, SampleCommandConfiguration(commandKey, threadPoolKey, groupKey, commandMetrics.Properties));
            }

            return commandConfigPerKey;
        };

    private static Func<long, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> AllThreadPoolConfig { get; } =
        _ =>
        {
            var threadPoolConfigPerKey = new Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>();
            foreach (var threadPoolMetrics in HystrixThreadPoolMetrics.GetInstances())
            {
                var threadPoolKey = threadPoolMetrics.ThreadPoolKey;
                threadPoolConfigPerKey.Add(threadPoolKey, SampleThreadPoolConfiguration(threadPoolKey, threadPoolMetrics.Properties));
            }

            return threadPoolConfigPerKey;
        };

    private static Func<long, Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> AllCollapserConfig { get; } =
        _ =>
        {
            var collapserConfigPerKey = new Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>();
            foreach (var collapserMetrics in HystrixCollapserMetrics.GetInstances())
            {
                var collapserKey = collapserMetrics.CollapserKey;
                collapserConfigPerKey.Add(collapserKey, SampleCollapserConfiguration(collapserKey, collapserMetrics.Properties));
            }

            return collapserConfigPerKey;
        };

    private static Func<HystrixConfiguration, Dictionary<IHystrixCommandKey, HystrixCommandConfiguration>> OnlyCommandConfig { get; } =
        hystrixConfiguration => hystrixConfiguration.CommandConfig;

    private static Func<HystrixConfiguration, Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration>> OnlyThreadPoolConfig { get; } =
        hystrixConfiguration => hystrixConfiguration.ThreadPoolConfig;

    private static Func<HystrixConfiguration, Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration>> OnlyCollapserConfig { get; } =
        hystrixConfiguration => hystrixConfiguration.CollapserConfig;
}
