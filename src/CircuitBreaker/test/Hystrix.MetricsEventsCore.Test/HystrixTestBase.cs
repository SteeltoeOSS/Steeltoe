// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Strategy;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using Steeltoe.CircuitBreaker.Hystrix.ThreadPool;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;

public abstract class HystrixTestBase : IDisposable
{
    protected HystrixTestBase()
    {
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HystrixThreadPoolFactory.Shutdown();
        }
    }
}
