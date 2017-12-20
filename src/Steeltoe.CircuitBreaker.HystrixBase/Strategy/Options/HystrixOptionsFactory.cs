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
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options
{
    public class HystrixOptionsFactory
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
