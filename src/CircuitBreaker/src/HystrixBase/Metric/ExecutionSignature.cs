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
