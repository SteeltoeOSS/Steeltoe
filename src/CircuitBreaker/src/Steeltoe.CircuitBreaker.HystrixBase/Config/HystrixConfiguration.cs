// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixConfiguration
    {
        private readonly Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfig;
        private readonly Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfig;
        private readonly Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfig;

        public HystrixConfiguration(
            Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfig,
            Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfig,
            Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfig)
        {
            this.commandConfig = commandConfig;
            this.threadPoolConfig = threadPoolConfig;
            this.collapserConfig = collapserConfig;
        }

        public static HystrixConfiguration From(
            Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> commandConfig,
            Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> threadPoolConfig,
            Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> collapserConfig)
        {
            return new HystrixConfiguration(commandConfig, threadPoolConfig, collapserConfig);
        }

        public Dictionary<IHystrixCommandKey, HystrixCommandConfiguration> CommandConfig
        {
            get { return commandConfig; }
        }

        public Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolConfiguration> ThreadPoolConfig
        {
             get { return threadPoolConfig; }
        }

        public Dictionary<IHystrixCollapserKey, HystrixCollapserConfiguration> CollapserConfig
        {
            get { return collapserConfig; }
        }
    }
}
