// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCommandExecutionStarted : HystrixCommandEvent
    {
        private readonly ExecutionIsolationStrategy _isolationStrategy;
        private readonly int _currentConcurrency;

        public HystrixCommandExecutionStarted(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy, int currentConcurrency)
            : base(commandKey, threadPoolKey)
        {
            this._isolationStrategy = isolationStrategy;
            this._currentConcurrency = currentConcurrency;
        }

        public override bool IsExecutionStart
        {
            get { return true; }
        }

        public override bool IsExecutedInThread
        {
            get { return _isolationStrategy == ExecutionIsolationStrategy.THREAD; }
        }

        public override bool IsResponseThreadPoolRejected
        {
            get { return false; }
        }

        public override bool IsCommandCompletion
        {
            get { return false; }
        }

        public override bool DidCommandExecute
        {
            get { return false; }
        }

        public int CurrentConcurrency
        {
            get { return _currentConcurrency; }
        }
    }
}
