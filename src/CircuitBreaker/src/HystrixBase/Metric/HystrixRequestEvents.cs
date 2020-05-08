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

using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class HystrixRequestEvents
    {
        public HystrixRequestEvents(ICollection<IHystrixInvokableInfo> executions)
        {
            Executions = executions;
        }

        public ICollection<IHystrixInvokableInfo> Executions { get; }

        public IDictionary<ExecutionSignature, List<int>> ExecutionsMappedToLatencies
        {
            get
            {
                Dictionary<CommandAndCacheKey, int> cachingDetector = new Dictionary<CommandAndCacheKey, int>();
                List<IHystrixInvokableInfo> nonCachedExecutions = new List<IHystrixInvokableInfo>(Executions.Count);
                foreach (IHystrixInvokableInfo execution in Executions)
                {
                    if (execution.PublicCacheKey != null)
                    {
                        // eligible for caching - might be the initial, or might be from cache
                        CommandAndCacheKey key = new CommandAndCacheKey(execution.CommandKey.Name, execution.PublicCacheKey);
                        int count = -1;
                        if (cachingDetector.TryGetValue(key, out count))
                        {
                            // key already seen
                            cachingDetector[key] = count + 1;
                        }
                        else
                        {
                            // key not seen yet
                            cachingDetector.Add(key, 0);
                        }
                    }

                    if (!execution.IsResponseFromCache)
                    {
                        nonCachedExecutions.Add(execution);
                    }
                }

                Dictionary<ExecutionSignature, List<int>> commandDeduper = new Dictionary<ExecutionSignature, List<int>>();
                foreach (IHystrixInvokableInfo execution in nonCachedExecutions)
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
                        // this has a RESPONSE_FROM_CACHE and needs to get split off
                        signature = ExecutionSignature.From(execution, cacheKey, cachedCount);
                    }
                    else
                    {
                        // nothing cached from this, can collapse further
                        signature = ExecutionSignature.From(execution);
                    }

                    if (commandDeduper.TryGetValue(signature, out List<int> currentLatencyList))
                    {
                        currentLatencyList.Add(execution.ExecutionTimeInMilliseconds);
                    }
                    else
                    {
                        List<int> newLatencyList = new List<int>
                        {
                            execution.ExecutionTimeInMilliseconds
                        };
                        commandDeduper.Add(signature, newLatencyList);
                    }
                }

                return commandDeduper;
            }
        }
    }
}