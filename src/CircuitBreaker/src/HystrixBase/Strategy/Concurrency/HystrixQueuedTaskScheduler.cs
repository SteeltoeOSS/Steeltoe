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

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixQueuedTaskScheduler : HystrixTaskScheduler
    {
        protected BlockingCollection<Task> workQueue;

        [ThreadStatic]
        private static bool isHystrixThreadPoolThread;

        private readonly object _lock = new object();

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

            this.workQueue = new BlockingCollection<Task>(queueSize);

            StartThreadPoolWorker();
            runningThreads = 1;
        }

        #region IHystrixTaskScheduler
        public override int CurrentQueueSize
        {
            get
            {
                return workQueue.Count;
            }
        }

        public override bool IsQueueSpaceAvailable
        {
            get { return workQueue.Count < queueSizeRejectionThreshold;  }
        }

        #endregion IHystrixTaskScheduler

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return workQueue.ToList();
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
                isHystrixThreadPoolThread = true;
#pragma warning restore S2696 // Instance members should not write to "static" fields
                try
                {
                    while (!this.shutdown)
                    {
                        workQueue.TryTake(out Task item, 250);

                        if (item != null)
                        {
                            try
                            {
                                Interlocked.Increment(ref this.runningTasks);
                                TryExecuteTask(item);
                            }
                            catch (Exception)
                            {
                                // Log
                            }
                            finally
                            {
                                Interlocked.Decrement(ref this.runningTasks);
                                Interlocked.Increment(ref completedTasks);
                            }
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref runningThreads);
                    isHystrixThreadPoolThread = false;
                }
            }, null);
        }

        protected override bool TryExecuteTaskInline(Task task, bool prevQueued)
        {
            if (!isHystrixThreadPoolThread)
            {
                return false;
            }

            if (prevQueued)
            {
                return false;
            }

            try
            {
                Interlocked.Increment(ref this.runningTasks);
                return TryExecuteTask(task);
            }
            catch (Exception)
            {
                // Log
            }
            finally
            {
                Interlocked.Decrement(ref this.runningTasks);
                Interlocked.Increment(ref completedTasks);
            }

            return true;
        }
    }
}
