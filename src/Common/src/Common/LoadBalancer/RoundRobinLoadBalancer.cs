// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Common.LoadBalancer;

public class RoundRobinLoadBalancer : ILoadBalancer
{
    public const string IndexKeyPrefix = "LoadBalancerIndex-";
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private readonly ILogger _logger;
    internal readonly IServiceInstanceProvider ServiceInstanceProvider;
    internal readonly IDistributedCache DistributedCache;
    internal readonly ConcurrentDictionary<string, int> NextIndexForService = new();

    public RoundRobinLoadBalancer(IServiceInstanceProvider serviceInstanceProvider, IDistributedCache distributedCache = null,
        DistributedCacheEntryOptions cacheEntryOptions = null, ILogger logger = null)
    {
        ServiceInstanceProvider = serviceInstanceProvider ?? throw new ArgumentNullException(nameof(serviceInstanceProvider));
        DistributedCache = distributedCache;
        _cacheOptions = cacheEntryOptions;
        _logger = logger;
        _logger?.LogDebug("Distributed cache was provided to load balancer: {DistributedCacheIsNull}", DistributedCache == null);
    }

    public virtual async Task<Uri> ResolveServiceInstanceAsync(Uri request)
    {
        string serviceName = request.Host;
        _logger?.LogTrace("ResolveServiceInstance {serviceName}", serviceName);
        string cacheKey = IndexKeyPrefix + serviceName;

        // get instances for this service
        IList<IServiceInstance> availableServiceInstances =
            await ServiceInstanceProvider.GetInstancesWithCacheAsync(serviceName, DistributedCache, _cacheOptions).ConfigureAwait(false);

        if (!availableServiceInstances.Any())
        {
            _logger?.LogError("No service instances available for {serviceName}", serviceName);
            return request;
        }

        // get next instance, or wrap back to first instance if we reach the end of the list
        int nextInstanceIndex = await GetOrInitNextIndexAsync(cacheKey, 0).ConfigureAwait(false);

        if (nextInstanceIndex >= availableServiceInstances.Count)
        {
            nextInstanceIndex = 0;
        }

        // get next instance, or wrap back to first instance if we reach the end of the list
        IServiceInstance serviceInstance = availableServiceInstances[nextInstanceIndex];
        _logger?.LogDebug("Resolved {url} to {service}", request.Host, serviceInstance.Host);
        await SetNextIndexAsync(cacheKey, nextInstanceIndex).ConfigureAwait(false);
        return new Uri(serviceInstance.Uri, request.PathAndQuery);
    }

    public virtual Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
    {
        return Task.CompletedTask;
    }

    private async Task<int> GetOrInitNextIndexAsync(string cacheKey, int initValue)
    {
        int index = initValue;

        if (DistributedCache != null)
        {
            byte[] cacheEntry = await DistributedCache.GetAsync(cacheKey).ConfigureAwait(false);

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

    private async Task SetNextIndexAsync(string cacheKey, int currentValue)
    {
        if (DistributedCache != null)
        {
            await DistributedCache.SetAsync(cacheKey, BitConverter.GetBytes(currentValue + 1)).ConfigureAwait(false);
        }
        else
        {
            NextIndexForService[cacheKey] = currentValue + 1;
        }
    }
}
