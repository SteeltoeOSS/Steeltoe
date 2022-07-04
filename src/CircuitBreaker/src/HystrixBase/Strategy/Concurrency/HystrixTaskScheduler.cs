// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public abstract class HystrixTaskScheduler : TaskScheduler, IHystrixTaskScheduler
{
    protected int innerCorePoolSize;
    protected TimeSpan innerKeepAliveTime;
    protected int innerMaximumPoolSize;
    protected int runningThreads;
    protected int queueSizeRejectionThreshold;
    protected bool shutdown;
    protected int queueSize;
    protected int runningTasks;
    protected int completedTasks;
    protected bool allowMaxToDivergeFromCore;

    private const int DefaultMinWorkthreads = 50;

    protected HystrixTaskScheduler(IHystrixThreadPoolOptions options)
    {
        if (options.MaximumSize < 1)
        {
            throw new ArgumentOutOfRangeException("maximumPoolSize");
        }

        if (options.CoreSize < 0)
        {
            throw new ArgumentOutOfRangeException("corePoolSize");
        }

        allowMaxToDivergeFromCore = options.AllowMaximumSizeToDivergeFromCoreSize;
        innerCorePoolSize = options.CoreSize;
        innerMaximumPoolSize = options.MaximumSize;
        innerKeepAliveTime = TimeSpan.FromMinutes(options.KeepAliveTimeMinutes);
        queueSize = options.MaxQueueSize;
        queueSizeRejectionThreshold = options.QueueSizeRejectionThreshold;

        System.Threading.ThreadPool.GetMinThreads(out var workThreads, out var compThreads);

        System.Threading.ThreadPool.SetMinThreads(Math.Max(workThreads, DefaultMinWorkthreads), compThreads);
    }

    public virtual int CurrentActiveCount => runningTasks;

    public virtual int CurrentCompletedTaskCount => completedTasks;

    public virtual int CurrentCorePoolSize => innerCorePoolSize;

    public virtual int CurrentLargestPoolSize => innerCorePoolSize;

    public virtual int CurrentMaximumPoolSize => innerCorePoolSize;

    public virtual int CurrentPoolSize => runningThreads;

    public virtual int CurrentQueueSize => 0;

    public virtual int CurrentTaskCount => runningTasks;

    public virtual int CorePoolSize
    {
        get => innerCorePoolSize;

        set => throw new NotImplementedException();
    }

    public virtual int MaximumPoolSize
    {
        get => innerMaximumPoolSize;

        set => throw new NotImplementedException();
    }

    public virtual TimeSpan KeepAliveTime
    {
        get => innerKeepAliveTime;

        set => throw new NotImplementedException();
    }

    public virtual bool IsQueueSpaceAvailable => false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public override int MaximumConcurrencyLevel => innerMaximumPoolSize;

    public bool IsShutdown => shutdown;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            shutdown = true;

            _ = Time.WaitUntil(() => runningThreads <= 0, 500);
        }
    }

    protected void RunContinuation(Task task)
    {
        System.Threading.ThreadPool.QueueUserWorkItem(
            t =>
            {
                if (t is Task item)
                {
                    try
                    {
                        TryExecuteTask(item);
                    }
                    catch (Exception)
                    {
                        // Log
                    }
                }
            }, task);
    }
}
