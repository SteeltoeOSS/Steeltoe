// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public class HystrixQueuedTaskScheduler : HystrixTaskScheduler
{
    protected BlockingCollection<Task> workQueue;

    [ThreadStatic]
    private static bool _isHystrixThreadPoolThread;

    private readonly object _lock = new ();

    public HystrixQueuedTaskScheduler(IHystrixThreadPoolOptions options)
        : base(options)
    {
        if (options.MaxQueueSize < 0)
        {
            throw new ArgumentOutOfRangeException("MaxQueueSize");
        }

        if (options.QueueSizeRejectionThreshold < 0)
        {
            throw new ArgumentOutOfRangeException("queueSizeRejectionThreshold");
        }

        workQueue = new BlockingCollection<Task>(queueSize);

        StartThreadPoolWorker();
        runningThreads = 1;
    }

    public override int CurrentQueueSize
    {
        get
        {
            return workQueue.Count;
        }
    }

    public override bool IsQueueSpaceAvailable
    {
        get { return workQueue.Count < queueSizeRejectionThreshold; }
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return workQueue.ToList();
    }

    protected override void QueueTask(Task task)
    {
        var isCommand = task.AsyncState is IHystrixInvokable;
        if (!isCommand)
        {
            RunContinuation(task);
            return;
        }

        if (runningThreads < corePoolSize)
        {
            lock (_lock)
            {
                if (runningThreads < corePoolSize)
                {
                    Interlocked.Increment(ref runningThreads);
                    StartThreadPoolWorker();
                }
            }
        }
        else if (allowMaxToDivergeFromCore && runningThreads < maximumPoolSize)
        {
            lock (_lock)
            {
                if (runningThreads < maximumPoolSize)
                {
                    Interlocked.Increment(ref runningThreads);
                    StartThreadPoolWorker();
                }
            }
        }

        if (!IsQueueSpaceAvailable)
        {
            throw new RejectedExecutionException("Rejected command because task queue queueSize is at rejection threshold.");
        }

        if (!workQueue.TryAdd(task))
        {
            throw new RejectedExecutionException("Rejected command because task work queue rejected add.");
        }
    }

    protected void StartThreadPoolWorker()
    {
        System.Threading.ThreadPool.QueueUserWorkItem(
            _ =>
            {
#pragma warning disable S2696 // Instance members should not write to "static" fields
                _isHystrixThreadPoolThread = true;
#pragma warning restore S2696 // Instance members should not write to "static" fields
                try
                {
                    while (!shutdown)
                    {
                        workQueue.TryTake(out var item, 250);

                        if (item != null)
                        {
                            try
                            {
                                Interlocked.Increment(ref runningTasks);
                                TryExecuteTask(item);
                            }
                            catch (Exception)
                            {
                                // Log
                            }
                            finally
                            {
                                Interlocked.Decrement(ref runningTasks);
                                Interlocked.Increment(ref completedTasks);
                            }
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref runningThreads);
                    _isHystrixThreadPoolThread = false;
                }
            }, null);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!_isHystrixThreadPoolThread)
        {
            return false;
        }

        if (taskWasPreviouslyQueued)
        {
            return false;
        }

        try
        {
            Interlocked.Increment(ref runningTasks);
            return TryExecuteTask(task);
        }
        catch (Exception)
        {
            // Log
        }
        finally
        {
            Interlocked.Decrement(ref runningTasks);
            Interlocked.Increment(ref completedTasks);
        }

        return true;
    }
}
