// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class ExecutionSignature
    {
        private readonly string commandName;
        private readonly ExecutionResult.EventCounts eventCounts;
        private readonly string cacheKey;
        private readonly int cachedCount;
        private readonly IHystrixCollapserKey collapserKey;
        private readonly int collapserBatchSize;

        private ExecutionSignature(IHystrixCommandKey commandKey, ExecutionResult.EventCounts eventCounts, string cacheKey, int cachedCount, IHystrixCollapserKey collapserKey, int collapserBatchSize)
        {
            this.commandName = commandKey.Name;
            this.eventCounts = eventCounts;
            this.cacheKey = cacheKey;
            this.cachedCount = cachedCount;
            this.collapserKey = collapserKey;
            this.collapserBatchSize = collapserBatchSize;
        }

        public static ExecutionSignature From(IHystrixInvokableInfo execution)
        {
            return new ExecutionSignature(execution.CommandKey, execution.EventCounts, null, 0, execution.OriginatingCollapserKey, execution.NumberCollapsed);
        }

        public static ExecutionSignature From(IHystrixInvokableInfo execution, string cacheKey, int cachedCount)
        {
            return new ExecutionSignature(execution.CommandKey, execution.EventCounts, cacheKey, cachedCount, execution.OriginatingCollapserKey, execution.NumberCollapsed);
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }

            if (o == null || GetType() != o.GetType())
            {
                return false;
            }

            ExecutionSignature that = (ExecutionSignature)o;

            if (!commandName.Equals(that.commandName))
            {
                return false;
            }

            if (!eventCounts.Equals(that.eventCounts))
            {
                return false;
            }

            return !(cacheKey != null ? !cacheKey.Equals(that.cacheKey) : that.cacheKey != null);
        }

        public override int GetHashCode()
        {
            int result = commandName.GetHashCode();
            result = (31 * result) + eventCounts.GetHashCode();
            result = (31 * result) + (cacheKey != null ? cacheKey.GetHashCode() : 0);
            return result;
        }

        public string CommandName
        {
            get { return commandName; }
        }

        public ExecutionResult.EventCounts Eventcounts
        {
            get { return eventCounts; }
        }

        public int CachedCount
        {
            get { return cachedCount; }
        }

        public IHystrixCollapserKey CollapserKey
        {
            get { return collapserKey; }
        }

        public int CollapserBatchSize
        {
            get { return collapserBatchSize; }
        }
    }
}
