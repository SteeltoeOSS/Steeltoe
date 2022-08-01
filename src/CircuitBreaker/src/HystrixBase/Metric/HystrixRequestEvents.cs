// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

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
            var cachingDetector = new Dictionary<CommandAndCacheKey, int>();
            var nonCachedExecutions = new List<IHystrixInvokableInfo>(Executions.Count);
            foreach (var execution in Executions)
            {
                if (execution.PublicCacheKey != null)
                {
                    // eligible for caching - might be the initial, or might be from cache
                    var key = new CommandAndCacheKey(execution.CommandKey.Name, execution.PublicCacheKey);
                    if (cachingDetector.TryGetValue(key, out var count))
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

            var commandDeduper = new Dictionary<ExecutionSignature, List<int>>();
            foreach (var execution in nonCachedExecutions)
            {
                var cachedCount = 0;
                var cacheKey = execution.PublicCacheKey;
                if (cacheKey != null)
                {
                    var key = new CommandAndCacheKey(execution.CommandKey.Name, cacheKey);
                    cachingDetector.TryGetValue(key, out cachedCount);
                }

                var signature = cachedCount > 0

                    // this has a RESPONSE_FROM_CACHE and needs to get split off
                    ? ExecutionSignature.From(execution, cacheKey, cachedCount)

                    // nothing cached from this, can collapse further
                    : ExecutionSignature.From(execution);

                if (commandDeduper.TryGetValue(signature, out var currentLatencyList))
                {
                    currentLatencyList.Add(execution.ExecutionTimeInMilliseconds);
                }
                else
                {
                    var newLatencyList = new List<int>
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
