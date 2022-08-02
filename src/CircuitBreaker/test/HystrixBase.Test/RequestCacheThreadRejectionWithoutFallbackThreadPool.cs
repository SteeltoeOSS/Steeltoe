// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class RequestCacheThreadRejectionWithoutFallbackThreadPool : IHystrixThreadPool
{
    private readonly IHystrixTaskScheduler _scheduler = new RequestCacheThreadRejectionWithoutFallbackTaskScheduler(new HystrixThreadPoolOptions());

    public bool IsQueueSpaceAvailable => false;

    public bool IsShutdown => _scheduler.IsShutdown;

    public IHystrixTaskScheduler GetScheduler()
    {
        return _scheduler;
    }

    public TaskScheduler GetTaskScheduler()
    {
        return _scheduler as TaskScheduler;
    }

    public void MarkThreadExecution()
    {
        // not used for this test
    }

    public void MarkThreadCompletion()
    {
        // not used for this test
    }

    public void MarkThreadRejection()
    {
        // not used for this test
    }

    public void Dispose()
    {
    }
}
