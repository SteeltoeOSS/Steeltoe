// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class SingleThreadedPoolWithQueue : IHystrixThreadPool
{
    private readonly HystrixThreadPoolOptions _options;
    private readonly IHystrixTaskScheduler _scheduler;

    public SingleThreadedPoolWithQueue(int queueSize)
        : this(queueSize, 100)
    {
    }

    public SingleThreadedPoolWithQueue(int queueSize, int rejectionQueueSizeThreshold)
    {
        _options = new HystrixThreadPoolOptions
        {
            MaxQueueSize = queueSize,
            CoreSize = 1,
            MaximumSize = 1,
            KeepAliveTimeMinutes = 1,
            QueueSizeRejectionThreshold = rejectionQueueSizeThreshold
        };
        _scheduler = new HystrixQueuedTaskScheduler(_options);
    }

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
        _scheduler.Dispose();
    }

    public bool IsQueueSpaceAvailable
    {
        get { return _scheduler.IsQueueSpaceAvailable; }
    }

    public int CurrentQueueSize
    {
        get { return _scheduler.CurrentQueueSize; }
    }

    public bool IsShutdown
    {
        get { return _scheduler.IsShutdown; }
    }
}
