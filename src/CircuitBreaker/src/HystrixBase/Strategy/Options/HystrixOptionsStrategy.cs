// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

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
