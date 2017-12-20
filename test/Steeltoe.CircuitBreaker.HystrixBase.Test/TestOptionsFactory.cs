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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    internal class TestOptionsFactory : HystrixOptionsStrategy
    {
        public override IHystrixCommandOptions GetCommandOptions(IHystrixCommandKey commandKey, IHystrixCommandOptions builder)
        {
            if (builder == null)
            {
                builder = HystrixCommandOptionsTest.GetUnitTestOptions();
            }

            return builder;
        }

        public override IHystrixThreadPoolOptions GetThreadPoolOptions(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions builder)
        {
            if (builder == null)
            {
                builder = HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder();
            }

            return builder;
        }

        public override IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions builder)
        {
            throw new InvalidOperationException("not expecting collapser properties");
        }

        public override string GetCommandOptionsCacheKey(IHystrixCommandKey commandKey, IHystrixCommandOptions builder)
        {
            return null;
        }

        public override string GetThreadPoolOptionsCacheKey(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions builder)
        {
            return null;
        }

        public override string GetCollapserOptionsCacheKey(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions builder)
        {
            return null;
        }
    }
}
