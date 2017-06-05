//
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
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixQueuedTaskScheduler : HystrixTaskScheduler
    {

        protected BlockingCollection<Task> workQueue;

        [ThreadStatic]
        private static bool isHystrixThreadPoolThread;

        public HystrixQueuedTaskScheduler(IHystrixThreadPoolOptions options) :
            base(options)
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

            if (runningThreads < corePoolSize)
            {
                Interlocked.Increment(ref runningThreads);
                StartThreadPoolWorker();
            } else if (allowMaxToDivergeFromCore && runningThreads < maximumPoolSize )
            {
                Interlocked.Increment(ref runningThreads);
                StartThreadPoolWorker();
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

        protected virtual void StartThreadPoolWorker()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                isHystrixThreadPoolThread = true;
                try
                {
                    while (!this.shutdown)
                    {
                        Task item = null;

                        if (!workQueue.TryTake(out item, 250))
                        {
                            if (item == null && 
                                (runningThreads > corePoolSize || 
                                this.shutdown))
                            {
                                break;
                            }
                        }
                        if (item != null)
                        {
                            try
                            {
                                Interlocked.Increment(ref this.runningTasks);
                                base.TryExecuteTask(item);
       
                            } catch(Exception)
                            {
                                // Log
                            } finally
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
                return base.TryExecuteTask(task);
            } catch (Exception)
            {
                // Log
            } finally
            {
                Interlocked.Decrement(ref this.runningTasks);
                Interlocked.Increment(ref completedTasks);
            }
            return true;
        }

    }
}
