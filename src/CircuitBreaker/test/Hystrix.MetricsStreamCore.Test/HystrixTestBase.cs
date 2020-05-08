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

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixTestBase : IDisposable
    {
        protected HystrixRequestContext context;

        public HystrixTestBase()
        {
            context = HystrixRequestContext.InitializeContext();

            HystrixCommandMetrics.Reset();
            HystrixThreadPoolMetrics.Reset();
            HystrixCollapserMetrics.Reset();

            // clear collapsers
            RequestCollapserFactory.Reset();

            // clear circuit breakers
            HystrixCircuitBreakerFactory.Reset();
            HystrixPlugins.Reset();
            HystrixOptionsFactory.Reset();
        }

        public virtual void Dispose()
        {
            if (context != null)
            {
                context.Dispose();
                context = null;
            }

            HystrixThreadPoolFactory.Shutdown();
        }
    }
}
