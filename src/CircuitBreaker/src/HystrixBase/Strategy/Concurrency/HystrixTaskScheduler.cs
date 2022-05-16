// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
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
        protected int queueSizeRejectionThreshold;
        protected bool shutdown;
        protected int queueSize;
        protected int runningTasks = 0;
        protected int completedTasks = 0;
        protected bool allowMaxToDivergeFromCore;

        private const int DEFAULT_MIN_WORKTHREADS = 50;

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
            corePoolSize = options.CoreSize;
            maximumPoolSize = options.MaximumSize;
            keepAliveTime = TimeSpan.FromMinutes(options.KeepAliveTimeMinutes);
            queueSize = options.MaxQueueSize;
            queueSizeRejectionThreshold = options.QueueSizeRejectionThreshold;

            System.Threading.ThreadPool.GetMinThreads(out var workThreads, out var compThreads);

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

                _ = Time.WaitUntil(() => { return runningThreads <= 0; }, 500);
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
