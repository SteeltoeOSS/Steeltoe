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

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixSyncTaskScheduler : HystrixTaskScheduler
    {
        [ThreadStatic]
        private static bool isHystrixThreadPoolThread;

        [ThreadStatic]
        private static ThreadTaskQueue workQueue;

        private ThreadTaskQueue[] workQueues;

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
                if (!workQueues[i].ThreadAssigned)
                {
                    lock (workQueues[i])
                    {
                        if (!workQueues[i].ThreadAssigned)
                        {
                            workQueues[i].ThreadAssigned = true;
                            StartThreadPoolWorker(workQueues[i]);
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
            System.Threading.ThreadPool.QueueUserWorkItem(
                (queue) =>
            {
                isHystrixThreadPoolThread = true;
                workQueue = queue as ThreadTaskQueue;
                workQueue.ThreadStartTime = Time.CurrentTimeMillis;

                try
                {
                    while (!this.shutdown)
                    {
                        Task item = null;
                        workQueue.Signal.Wait(250);

                        item = workQueue.Task;

                        if (item != null)
                        {
                            try
                            {
                                Interlocked.Increment(ref this.runningTasks);
                                TryExecuteTask(item);
                            }
                            catch (Exception)
                            {
                                // log
                            }
                            finally
                            {
                                Interlocked.Decrement(ref this.runningTasks);
                                Interlocked.Increment(ref completedTasks);
                            }

                            workQueue.Signal.Reset();
                            workQueue.Task = null;
                        }
                    }
                }
                finally
                {
                    isHystrixThreadPoolThread = false;
                    workQueue.Signal.Reset();
                    workQueue.ThreadAssigned = false;
                    Interlocked.Decrement(ref runningThreads);
                    workQueue = null;
                }
            }, input);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return new List<Task>();
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

        protected virtual bool TryAddToAny(Task task)
        {
            foreach (ThreadTaskQueue queue in workQueues)
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

        protected virtual void SetupWorkQueues(int size)
        {
            workQueues = new ThreadTaskQueue[size];
            for (int i = 0; i < size; i++)
            {
                workQueues[i] = new ThreadTaskQueue();
            }
        }

        #region IHystrixTaskScheduler
        public override int CurrentQueueSize
        {
            get
            {
                int size = 0;
                for (int i = 0; i < workQueues.Length; i++)
                {
                    ThreadTaskQueue queue = workQueues[i];
                    if (queue.ThreadAssigned && queue.Task != null)
                    {
                        size++;
                    }
                }

                return size;
            }
        }

        public override bool IsQueueSpaceAvailable
        {
            get { return CurrentQueueSize < workQueues.Length;  }
        }

        #endregion IHystrixTaskScheduler

        public class ThreadTaskQueue
        {
            public ThreadTaskQueue()
            {
                Signal = new ManualResetEventSlim(false);
                Task = null;
                ThreadAssigned = false;
            }

            public ManualResetEventSlim Signal;
            public Task Task;
            public bool ThreadAssigned;
            public long ThreadStartTime;
            public long WorkerStartTime;
        }
    }
}
