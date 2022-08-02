// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

public static class HystrixOptionsFactory
{
    private static readonly ConcurrentDictionary<string, IHystrixCommandOptions> CommandProperties = new();

    private static readonly ConcurrentDictionary<string, IHystrixThreadPoolOptions> ThreadPoolProperties = new();

    private static readonly ConcurrentDictionary<string, IHystrixCollapserOptions> CollapserProperties = new();

    public static void Reset()
    {
        CommandProperties.Clear();
        ThreadPoolProperties.Clear();
        CollapserProperties.Clear();
    }

    public static IHystrixCommandOptions GetCommandOptions(IHystrixCommandKey key, IHystrixCommandOptions builder)
    {
        HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        string cacheKey = hystrixPropertiesStrategy.GetCommandOptionsCacheKey(key, builder);

        if (cacheKey != null)
        {
            return CommandProperties.GetOrAddEx(cacheKey, _ => hystrixPropertiesStrategy.GetCommandOptions(key, builder));
        }

        // no cacheKey so we generate it with caching
        return hystrixPropertiesStrategy.GetCommandOptions(key, builder);
    }

    public static IHystrixThreadPoolOptions GetThreadPoolOptions(IHystrixThreadPoolKey key, IHystrixThreadPoolOptions builder)
    {
        HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        string cacheKey = hystrixPropertiesStrategy.GetThreadPoolOptionsCacheKey(key, builder);

        if (cacheKey != null)
        {
            return ThreadPoolProperties.GetOrAddEx(cacheKey, _ => hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder));
        }

        // no cacheKey so we generate it with caching
        return hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder);
    }

    public static IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey key, IHystrixCollapserOptions builder)
    {
        HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        string cacheKey = hystrixPropertiesStrategy.GetCollapserOptionsCacheKey(key, builder);

        if (cacheKey != null)
        {
            return CollapserProperties.GetOrAddEx(cacheKey, _ => hystrixPropertiesStrategy.GetCollapserOptions(key, builder));
        }

        // no cacheKey so we generate it with caching
        return hystrixPropertiesStrategy.GetCollapserOptions(key, builder);
    }
}
