// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
