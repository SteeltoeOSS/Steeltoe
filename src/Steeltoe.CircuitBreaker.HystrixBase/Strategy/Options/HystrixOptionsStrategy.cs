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

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options
{
    public abstract class HystrixOptionsStrategy
    {
        public virtual IHystrixCommandOptions GetCommandOptions(IHystrixCommandKey commandKey, IHystrixCommandOptions builder)
        {
            return new HystrixCommandOptions(commandKey, builder);
        }

        public virtual string GetCommandOptionsCacheKey(IHystrixCommandKey commandKey, IHystrixCommandOptions builder)
        {
            return commandKey.Name;
        }

        public virtual IHystrixThreadPoolOptions GetThreadPoolOptions(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions builder)
        {
            return new HystrixThreadPoolOptions(threadPoolKey, builder);
        }

        public virtual string GetThreadPoolOptionsCacheKey(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions builder)
        {
            return threadPoolKey.Name;
        }

        public virtual IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions builder)
        {
            return new HystrixCollapserOptions(collapserKey, builder);
        }

        public virtual string GetCollapserOptionsCacheKey(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions builder)
        {
            return collapserKey.Name;
        }

        public virtual IHystrixDynamicOptions GetDynamicOptions(IConfiguration configSource)
        {
            return new HystrixDynamicOptionsDefault(configSource);
        }
    }
}
