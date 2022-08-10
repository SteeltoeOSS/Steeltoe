// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

public class HystrixSyncTaskScheduler : HystrixTaskScheduler
{
    [ThreadStatic]
    private static bool _isHystrixThreadPoolThread;

    [ThreadStatic]
    private static ThreadTaskQueue _workQueue;

    private ThreadTaskQueue[] _workQueues;

    public override int CurrentQueueSize
    {
        get
        {
            int size = 0;

            foreach (ThreadTaskQueue queue in _workQueues)
            {
                if (queue.ThreadAssigned && queue.Task != null)
                {
                    size++;
                }
            }

            return size;
        }
    }

    public override bool IsQueueSpaceAvailable => CurrentQueueSize < _workQueues.Length;

    public HystrixSyncTaskScheduler(IHystrixThreadPoolOptions options)
        : base(options)
    {
        SetupWorkQueues(corePoolSize);
    }

    protected override void QueueTask(Task task)
    {
        bool isCommand = task.AsyncState is IHystrixInvokable;

        if (!isCommand)
        {
            RunContinuation(task);
            return;
        }

        if (runningThreads < corePoolSize)
        {
            StartThreadPoolWorker();
        }

        if (!TryAddToAny(task))
        {
            throw new RejectedExecutionException("Rejected command because task work queues rejected add.");
        }
    }

    protected virtual void StartThreadPoolWorker()
    {
        for (int i = 0; i < corePoolSize; i++)
        {
            if (!_workQueues[i].ThreadAssigned)
            {
                lock (_workQueues[i])
                {
                    if (!_workQueues[i].ThreadAssigned)
                    {
                        _workQueues[i].ThreadAssigned = true;
                        StartThreadPoolWorker(_workQueues[i]);
                        Interlocked.Increment(ref runningThreads);
                        break;
                    }
                }
            }
        }
    }

    protected virtual void StartThreadPoolWorker(ThreadTaskQueue input)
    {
        input.WorkerStartTime = Time.CurrentTimeMillis;

        System.Threading.ThreadPool.QueueUserWorkItem(queue =>
        {
#pragma warning disable S2696 // Instance members should not write to "static" fields
            _isHystrixThreadPoolThread = true;
            _workQueue = queue as ThreadTaskQueue;
#pragma warning restore S2696 // Instance members should not write to "static" fields
            _workQueue.ThreadStartTime = Time.CurrentTimeMillis;

            try
            {
                while (!shutdown)
                {
                    Task item = null;
                    _workQueue.Signal.Wait(250);

                    item = _workQueue.Task;

                    if (item != null)
                    {
                        try
                        {
                            Interlocked.Increment(ref runningTasks);
                            TryExecuteTask(item);
                        }
                        catch (Exception)
                        {
                            // log
                        }
                        finally
                        {
                            Interlocked.Decrement(ref runningTasks);
                            Interlocked.Increment(ref completedTasks);
                        }

                        _workQueue.Signal.Reset();
                        _workQueue.Task = null;
                    }
                }
            }
            finally
            {
                _isHystrixThreadPoolThread = false;
                _workQueue.Signal.Reset();
                _workQueue.ThreadAssigned = false;
                Interlocked.Decrement(ref runningThreads);
                _workQueue = null;
            }
        }, input);
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return new List<Task>();
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

    protected virtual bool TryAddToAny(Task task)
    {
        foreach (ThreadTaskQueue queue in _workQueues)
        {
            if (queue.ThreadAssigned && queue.Task == null)
            {
                lock (queue)
                {
                    if (queue.ThreadAssigned && queue.Task == null)
                    {
                        queue.Task = task;
                        queue.Signal.Set();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    protected void SetupWorkQueues(int size)
    {
        _workQueues = new ThreadTaskQueue[size];

        for (int i = 0; i < size; i++)
        {
            _workQueues[i] = new ThreadTaskQueue();
        }
    }

    public class ThreadTaskQueue
    {
        public ManualResetEventSlim Signal;
        public Task Task;
        public bool ThreadAssigned;
        public long ThreadStartTime;
        public long WorkerStartTime;

        public ThreadTaskQueue()
        {
            Signal = new ManualResetEventSlim(false);
            Task = null;
            ThreadAssigned = false;
        }
    }
}
