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

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCommandExecutionStarted : HystrixCommandEvent
    {
        private readonly ExecutionIsolationStrategy isolationStrategy;

        public HystrixCommandExecutionStarted(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy, int currentConcurrency)
            : base(commandKey, threadPoolKey)
        {
            this.isolationStrategy = isolationStrategy;
            CurrentConcurrency = currentConcurrency;
        }

        public override bool IsExecutionStart
        {
            get { return true; }
        }

        public override bool IsExecutedInThread
        {
            get { return isolationStrategy == ExecutionIsolationStrategy.THREAD; }
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

        public int CurrentConcurrency { get; }
    }
}
