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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixCommandCompletion : HystrixCommandEvent
    {
        protected readonly ExecutionResult executionResult;
        protected readonly HystrixRequestContext requestContext;

        private static readonly IList<HystrixEventType> ALL_EVENT_TYPES = HystrixEventTypeHelper.Values;

        internal HystrixCommandCompletion(
            ExecutionResult executionResult,
            IHystrixCommandKey commandKey,
            IHystrixThreadPoolKey threadPoolKey,
            HystrixRequestContext requestContext)
            : base(commandKey, threadPoolKey)
        {
            this.executionResult = executionResult;
            this.requestContext = requestContext;
        }

        public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
        {
            return From(executionResult, commandKey, threadPoolKey, HystrixRequestContext.ContextForCurrentThread);
        }

        public static HystrixCommandCompletion From(ExecutionResult executionResult, IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, HystrixRequestContext requestContext)
        {
            return new HystrixCommandCompletion(executionResult, commandKey, threadPoolKey, requestContext);
        }

        public override bool IsResponseThreadPoolRejected
        {
            get { return executionResult.IsResponseThreadPoolRejected; }
        }

        public override bool IsExecutionStart
        {
            get { return false; }
        }

        public override bool IsExecutedInThread
        {
            get { return executionResult.IsExecutedInThread; }
        }

        public override bool IsCommandCompletion
        {
            get { return true; }
        }

        public HystrixRequestContext RequestContext
        {
            get { return requestContext; }
        }

        public ExecutionResult.EventCounts Eventcounts
        {
            get { return executionResult.Eventcounts; }
        }

        public long ExecutionLatency
        {
            get { return executionResult.ExecutionLatency; }
        }

        public long TotalLatency
        {
            get { return executionResult.UserThreadLatency; }
        }

        public override bool DidCommandExecute
        {
            get { return executionResult.ExecutionOccurred; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var foundEventTypes = new List<HystrixEventType>();

            sb.Append(CommandKey.Name).Append("[");
            foreach (var eventType in ALL_EVENT_TYPES)
            {
                if (executionResult.Eventcounts.Contains(eventType))
                {
                    foundEventTypes.Add(eventType);
                }
            }

            var i = 0;
            foreach (var eventType in foundEventTypes)
            {
                sb.Append(eventType.ToString());
                var eventCount = executionResult.Eventcounts.GetCount(eventType);
                if (eventCount > 1)
                {
                    sb.Append("x").Append(eventCount);
                }

                if (i < foundEventTypes.Count - 1)
                {
                    sb.Append(", ");
                }

                i++;
            }

            sb.Append("][").Append(ExecutionLatency).Append(" ms]");
            return sb.ToString();
        }
    }
}
