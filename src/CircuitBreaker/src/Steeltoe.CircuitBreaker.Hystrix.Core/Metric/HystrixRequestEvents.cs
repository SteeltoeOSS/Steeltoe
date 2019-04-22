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
using System.Collections.Generic;


namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixRequestEvents
    {
        private readonly ICollection<IHystrixInvokableInfo> executions;

        public HystrixRequestEvents(ICollection<IHystrixInvokableInfo> executions)
        {
            this.executions = executions;
        }

        public ICollection<IHystrixInvokableInfo> Executions
        {
            get { return executions; }
        }

        public IDictionary<ExecutionSignature, List<int>> ExecutionsMappedToLatencies
        {
            get {
                Dictionary<CommandAndCacheKey, int> cachingDetector = new Dictionary<CommandAndCacheKey, int>();
                List <IHystrixInvokableInfo> nonCachedExecutions = new List<IHystrixInvokableInfo>(executions.Count);
                foreach (IHystrixInvokableInfo execution in executions)
                {
                    if (execution.PublicCacheKey != null)
                    {
                        //eligible for caching - might be the initial, or might be from cache
                        CommandAndCacheKey key = new CommandAndCacheKey(execution.CommandKey.Name, execution.PublicCacheKey);
                        int count = -1;
                        if (cachingDetector.TryGetValue(key, out count))
                        {
                            //key already seen
                            cachingDetector[key] =  count + 1;
                        }
                        else
                        {
                            //key not seen yet
                            cachingDetector.Add(key, 0);
                        }
                    }
                    if (!execution.IsResponseFromCache)
                    {
                        nonCachedExecutions.Add(execution);
                    }
                }

                Dictionary<ExecutionSignature, List<int>> commandDeduper = new Dictionary<ExecutionSignature, List<int>>();
                foreach (IHystrixInvokableInfo  execution in nonCachedExecutions)
                {
                    int cachedCount = 0;
                    string cacheKey = execution.PublicCacheKey;
                    if (cacheKey != null)
                    {
                        CommandAndCacheKey key = new CommandAndCacheKey(execution.CommandKey.Name, cacheKey);
                        cachingDetector.TryGetValue(key, out cachedCount);
                    }
                    ExecutionSignature signature;
                    if (cachedCount > 0)
                    {
                        //this has a RESPONSE_FROM_CACHE and needs to get split off
                        signature = ExecutionSignature.From(execution, cacheKey, cachedCount);
                    }
                    else
                    {
                        //nothing cached from this, can collapse further
                        signature = ExecutionSignature.From(execution);
                    }
                    List<int> currentLatencyList = null;
                    if (commandDeduper.TryGetValue(signature, out currentLatencyList))
                    {
                        currentLatencyList.Add(execution.ExecutionTimeInMilliseconds);
                    }
                    else
                    {
                        List<int> newLatencyList = new List<int>();
                        newLatencyList.Add(execution.ExecutionTimeInMilliseconds);
                        commandDeduper.Add(signature, newLatencyList);
                    }
                }

                return commandDeduper;
            }
        }


    }
    class CommandAndCacheKey
    {
        private readonly string commandName;
        private readonly string cacheKey;

        public CommandAndCacheKey(String commandName, String cacheKey)
        {
            this.commandName = commandName;
            this.cacheKey = cacheKey;
        }

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            CommandAndCacheKey that = (CommandAndCacheKey)o;

            if (!commandName.Equals(that.commandName)) return false;
            return cacheKey.Equals(that.cacheKey);

        }


        public override int GetHashCode()
        {
            int result = commandName.GetHashCode();
            result = 31 * result + cacheKey.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return "CommandAndCacheKey{" +
                    "commandName='" + commandName + '\'' +
                    ", cacheKey='" + cacheKey + '\'' +
                    '}';
        }
    }

    public class ExecutionSignature
    {
        private readonly string commandName;
        private readonly ExecutionResult.EventCounts eventCounts;
        private readonly string cacheKey;
        private readonly int cachedCount;
        private readonly IHystrixCollapserKey collapserKey;
        private readonly int collapserBatchSize;

        private ExecutionSignature(IHystrixCommandKey commandKey, ExecutionResult.EventCounts eventCounts, String cacheKey, int cachedCount, IHystrixCollapserKey collapserKey, int collapserBatchSize)
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


        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            ExecutionSignature that = (ExecutionSignature)o;

            if (!commandName.Equals(that.commandName)) return false;
            if (!eventCounts.Equals(that.eventCounts)) return false;
            return !(cacheKey != null ? !cacheKey.Equals(that.cacheKey) : that.cacheKey != null);

        }

        public override int GetHashCode()
        {
            int result = commandName.GetHashCode();
            result = 31 * result + eventCounts.GetHashCode();
            result = 31 * result + (cacheKey != null ? cacheKey.GetHashCode() : 0);
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

