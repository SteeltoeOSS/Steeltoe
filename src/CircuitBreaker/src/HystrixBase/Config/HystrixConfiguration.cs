// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixConfiguration
    {
        public HystrixConfiguration(
            Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfig,
            Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfig,
            Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfig)
        {
            CommandConfig = commandConfig;
            ThreadPoolConfig = threadPoolConfig;
            CollapserConfig = collapserConfig;
        }

        public static HystrixConfiguration From(
            Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfig,
            Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfig,
            Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfig)
        {
            return new HystrixConfiguration(commandConfig, threadPoolConfig, collapserConfig);
        }

        public Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> CommandConfig { get; }

        public Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> ThreadPoolConfig { get; }

        public Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> CollapserConfig { get; }
    }
}
