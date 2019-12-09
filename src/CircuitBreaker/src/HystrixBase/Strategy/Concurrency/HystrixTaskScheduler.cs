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

using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.CircuitBreaker.Util;
using System;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public abstract class HystrixTaskScheduler : TaskScheduler, IHystrixTaskScheduler
    {
        protected int corePoolSize;
        protected TimeSpan keepAliveTime;
        protected int maximumPoolSize;
        protected int runningThreads = 0;
        protected int queueSizeRejectionThreshold = 0;
        protected bool shutdown = false;
        protected int queueSize;
        protected int runningTasks = 0;
        protected int completedTasks = 0;
        protected bool allowMaxToDivergeFromCore;

        private const int DEFAULT_MIN_WORKTHREADS = 50;

        public HystrixTaskScheduler(IHystrixThreadPoolOptions options)
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
            corePoolSize = options.CoreSize;
            maximumPoolSize = options.MaximumSize;
            keepAliveTime = TimeSpan.FromMinutes(options.KeepAliveTimeMinutes);
            queueSize = options.MaxQueueSize;
            queueSizeRejectionThreshold = options.QueueSizeRejectionThreshold;

            System.Threading.ThreadPool.GetMinThreads(out int workThreads, out int compThreads);

            System.Threading.ThreadPool.SetMinThreads(Math.Max(workThreads, DEFAULT_MIN_WORKTHREADS), compThreads);
        }

        #region IHystrixTaskScheduler
        public virtual int CurrentActiveCount => runningTasks;

        public virtual int CurrentCompletedTaskCount => completedTasks;

        public virtual int CurrentCorePoolSize => corePoolSize;

        public virtual int CurrentLargestPoolSize => corePoolSize;

        public virtual int CurrentMaximumPoolSize => corePoolSize;

        public virtual int CurrentPoolSize => runningThreads;

        public virtual int CurrentQueueSize => 0;

        public virtual int CurrentTaskCount => runningTasks;

        public virtual int CorePoolSize
        {
            get => corePoolSize;

            set => throw new NotImplementedException();
        }

        public virtual int MaximumPoolSize
        {
            get => maximumPoolSize;

            set => throw new NotImplementedException();
        }

        public virtual TimeSpan KeepAliveTime
        {
            get => keepAliveTime;

            set => throw new NotImplementedException();
        }

        public virtual bool IsQueueSpaceAvailable => false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IHystrixTaskScheduler
        public override int MaximumConcurrencyLevel => maximumPoolSize;

        public bool IsShutdown => shutdown;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                shutdown = true;

                Time.WaitUntil(() => { return runningThreads <= 0; }, 500);
            }
        }

        protected void RunContinuation(Task task)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(
                (t) =>
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
}
