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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public abstract class HystrixCommandEvent : IHystrixEvent
    {
        private readonly IHystrixCommandKey commandKey;
        private readonly IHystrixThreadPoolKey threadPoolKey;

        protected HystrixCommandEvent(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
        {
            this.commandKey = commandKey;
            this.threadPoolKey = threadPoolKey;
        }

        public static Func<HystrixCommandEvent, bool> FilterCompletionsOnly { get; } = (commandEvent) =>
        {
            return commandEvent.IsCommandCompletion;
        };

        public static Func<HystrixCommandEvent, bool> FilterActualExecutions { get; } = (commandEvent) =>
        {
            return commandEvent.DidCommandExecute;
        };

        public virtual IHystrixCommandKey CommandKey
        {
            get { return commandKey; }
        }

        public virtual IHystrixThreadPoolKey ThreadPoolKey
        {
            get { return threadPoolKey; }
        }

        public abstract bool IsExecutionStart { get; }

        public abstract bool IsExecutedInThread { get; }

        public abstract bool IsResponseThreadPoolRejected { get; }

        public abstract bool IsCommandCompletion { get; }

        public abstract bool DidCommandExecute { get; }
    }
}
