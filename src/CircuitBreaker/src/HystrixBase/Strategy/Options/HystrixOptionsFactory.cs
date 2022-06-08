// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

public static class HystrixOptionsFactory
{
    public static void Reset()
    {
        CommandProperties.Clear();
        ThreadPoolProperties.Clear();
        CollapserProperties.Clear();
    }

    private static readonly ConcurrentDictionary<string, IHystrixCommandOptions> CommandProperties = new ();

    public static IHystrixCommandOptions GetCommandOptions(IHystrixCommandKey key, IHystrixCommandOptions builder)
    {
        var hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        var cacheKey = hystrixPropertiesStrategy.GetCommandOptionsCacheKey(key, builder);
        if (cacheKey != null)
        {
            return CommandProperties.GetOrAddEx(cacheKey, k => hystrixPropertiesStrategy.GetCommandOptions(key, builder));
        }
        else
        {
            // no cacheKey so we generate it with caching
            return hystrixPropertiesStrategy.GetCommandOptions(key, builder);
        }
    }

    private static readonly ConcurrentDictionary<string, IHystrixThreadPoolOptions> ThreadPoolProperties = new ();

    public static IHystrixThreadPoolOptions GetThreadPoolOptions(IHystrixThreadPoolKey key, IHystrixThreadPoolOptions builder)
    {
        var hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        var cacheKey = hystrixPropertiesStrategy.GetThreadPoolOptionsCacheKey(key, builder);
        if (cacheKey != null)
        {
            return ThreadPoolProperties.GetOrAddEx(cacheKey, k => hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder));
        }
        else
        {
            // no cacheKey so we generate it with caching
            return hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder);
        }
    }

    private static readonly ConcurrentDictionary<string, IHystrixCollapserOptions> CollapserProperties = new ();

    public static IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey key, IHystrixCollapserOptions builder)
    {
        var hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
        var cacheKey = hystrixPropertiesStrategy.GetCollapserOptionsCacheKey(key, builder);
        if (cacheKey != null)
        {
            return CollapserProperties.GetOrAddEx(cacheKey, k => hystrixPropertiesStrategy.GetCollapserOptions(key, builder));
        }
        else
        {
            // no cacheKey so we generate it with caching
            return hystrixPropertiesStrategy.GetCollapserOptions(key, builder);
        }
    }
}
