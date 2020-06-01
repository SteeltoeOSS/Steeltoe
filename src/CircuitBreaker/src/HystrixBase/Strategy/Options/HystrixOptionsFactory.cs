// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options
{
    public static class HystrixOptionsFactory
    {
        public static void Reset()
        {
            commandProperties.Clear();
            threadPoolProperties.Clear();
            collapserProperties.Clear();
        }

        private static ConcurrentDictionary<string, IHystrixCommandOptions> commandProperties = new ConcurrentDictionary<string, IHystrixCommandOptions>();

        public static IHystrixCommandOptions GetCommandOptions(IHystrixCommandKey key, IHystrixCommandOptions builder)
        {
            HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
            string cacheKey = hystrixPropertiesStrategy.GetCommandOptionsCacheKey(key, builder);
            if (cacheKey != null)
            {
                return commandProperties.GetOrAddEx(cacheKey, (k) => hystrixPropertiesStrategy.GetCommandOptions(key, builder));
            }
            else
            {
                // no cacheKey so we generate it with caching
                return hystrixPropertiesStrategy.GetCommandOptions(key, builder);
            }
        }

        private static ConcurrentDictionary<string, IHystrixThreadPoolOptions> threadPoolProperties = new ConcurrentDictionary<string, IHystrixThreadPoolOptions>();

        public static IHystrixThreadPoolOptions GetThreadPoolOptions(IHystrixThreadPoolKey key, IHystrixThreadPoolOptions builder)
        {
            HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
            string cacheKey = hystrixPropertiesStrategy.GetThreadPoolOptionsCacheKey(key, builder);
            if (cacheKey != null)
            {
                return threadPoolProperties.GetOrAddEx(cacheKey, (k) => hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder));
            }
            else
            {
                // no cacheKey so we generate it with caching
                return hystrixPropertiesStrategy.GetThreadPoolOptions(key, builder);
            }
        }

        private static ConcurrentDictionary<string, IHystrixCollapserOptions> collapserProperties = new ConcurrentDictionary<string, IHystrixCollapserOptions>();

        public static IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey key, IHystrixCollapserOptions builder)
        {
            HystrixOptionsStrategy hystrixPropertiesStrategy = HystrixPlugins.OptionsStrategy;
            string cacheKey = hystrixPropertiesStrategy.GetCollapserOptionsCacheKey(key, builder);
            if (cacheKey != null)
            {
                return collapserProperties.GetOrAddEx(cacheKey, (k) => hystrixPropertiesStrategy.GetCollapserOptions(key, builder));
            }
            else
            {
                // no cacheKey so we generate it with caching
                return hystrixPropertiesStrategy.GetCollapserOptions(key, builder);
            }
        }
    }
}
