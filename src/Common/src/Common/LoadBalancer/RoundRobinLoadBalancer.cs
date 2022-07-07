// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Common.LoadBalancer;

public class RoundRobinLoadBalancer : ILoadBalancer
{
    public string IndexKeyPrefix = "LoadBalancerIndex-";
    internal readonly IServiceInstanceProvider ServiceInstanceProvider;
    internal readonly IDistributedCache _distributedCache;
    internal readonly ConcurrentDictionary<string, int> NextIndexForService = new ();
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private readonly ILogger _logger;

    public RoundRobinLoadBalancer(IServiceInstanceProvider serviceInstanceProvider, IDistributedCache distributedCache = null, DistributedCacheEntryOptions cacheEntryOptions = null, ILogger logger = null)
    {
        ServiceInstanceProvider = serviceInstanceProvider ?? throw new ArgumentNullException(nameof(serviceInstanceProvider));
        _distributedCache = distributedCache;
        _cacheOptions = cacheEntryOptions;
        _logger = logger;
        _logger?.LogDebug("Distributed cache was provided to load balancer: {DistributedCacheIsNull}", _distributedCache == null);
    }

    public virtual async Task<Uri> ResolveServiceInstanceAsync(Uri request)
    {
        var serviceName = request.Host;
        _logger?.LogTrace("ResolveServiceInstance {serviceName}", serviceName);
        var cacheKey = IndexKeyPrefix + serviceName;

        // get instances for this service
        var availableServiceInstances = await ServiceInstanceProvider.GetInstancesWithCacheAsync(serviceName, _distributedCache, _cacheOptions).ConfigureAwait(false);
        if (!availableServiceInstances.Any())
        {
            _logger?.LogError("No service instances available for {serviceName}", serviceName);
            return request;
        }

        // get next instance, or wrap back to first instance if we reach the end of the list
        var nextInstanceIndex = await GetOrInitNextIndex(cacheKey, 0).ConfigureAwait(false);
        if (nextInstanceIndex >= availableServiceInstances.Count)
        {
            nextInstanceIndex = 0;
        }

        // get next instance, or wrap back to first instance if we reach the end of the list
        var serviceInstance = availableServiceInstances[nextInstanceIndex];
        _logger?.LogDebug("Resolved {url} to {service}", request.Host, serviceInstance.Host);
        await SetNextIndex(cacheKey, nextInstanceIndex).ConfigureAwait(false);
        return new Uri(serviceInstance.Uri, request.PathAndQuery);
    }

    public virtual Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
    {
        return Task.CompletedTask;
    }

    private async Task<int> GetOrInitNextIndex(string cacheKey, int initValue)
    {
        var index = initValue;
        if (_distributedCache != null)
        {
            var cacheEntry = await _distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
            if (cacheEntry != null && cacheEntry.Length > 0)
            {
                index = BitConverter.ToInt16(cacheEntry, 0);
            }
        }
        else
        {
            index = NextIndexForService.GetOrAdd(cacheKey, initValue);
        }

        return index;
    }

    private async Task SetNextIndex(string cacheKey, int currentValue)
    {
        if (_distributedCache != null)
        {
            await _distributedCache.SetAsync(cacheKey, BitConverter.GetBytes(currentValue + 1)).ConfigureAwait(false);
        }
        else
        {
            NextIndexForService[cacheKey] = currentValue + 1;
        }
    }
}
