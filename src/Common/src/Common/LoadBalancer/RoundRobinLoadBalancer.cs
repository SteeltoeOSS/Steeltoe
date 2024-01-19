// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Common.LoadBalancer;

/// <summary>
/// Returns service instances in round-robin fashion, optionally using distributed caching for service lookups and for determining the next instance.
/// </summary>
public sealed class RoundRobinLoadBalancer : ILoadBalancer
{
    private const string CacheKeyPrefix = "Steeltoe-LoadBalancerIndex-";
    private readonly IDiscoveryClient _discoveryClient;
    private readonly IDistributedCache? _distributedCache;
    private readonly DistributedCacheEntryOptions _cacheEntryOptions;
    private readonly ILogger<RoundRobinLoadBalancer> _logger;
    private readonly ConcurrentDictionary<string, int> _lastUsedIndexPerServiceName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinLoadBalancer" /> class.
    /// </summary>
    /// <param name="discoveryClient">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public RoundRobinLoadBalancer(IDiscoveryClient discoveryClient, ILogger<RoundRobinLoadBalancer> logger)
        : this(discoveryClient, null, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinLoadBalancer" /> class.
    /// </summary>
    /// <param name="discoveryClient">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="distributedCache">
    /// For caching service instances and the last-used instance.
    /// </param>
    /// <param name="cacheEntryOptions">
    /// Configuration for <paramref name="distributedCache" />.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public RoundRobinLoadBalancer(IDiscoveryClient discoveryClient, IDistributedCache? distributedCache, DistributedCacheEntryOptions? cacheEntryOptions,
        ILogger<RoundRobinLoadBalancer> logger)
    {
        ArgumentGuard.NotNull(discoveryClient);
        ArgumentGuard.NotNull(logger);

        _discoveryClient = discoveryClient;
        _distributedCache = distributedCache;
        _cacheEntryOptions = cacheEntryOptions ?? new DistributedCacheEntryOptions();
        _logger = logger;

        _logger.LogDebug("Distributed cache was provided to load balancer: {UseDistributedCache}.", _distributedCache == null);
    }

    /// <inheritdoc />
    public async Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(requestUri);

        string serviceName = requestUri.Host;
        _logger.LogTrace("Resolving service instance for '{serviceName}'.", serviceName);

        IList<IServiceInstance> availableServiceInstances =
            await _discoveryClient.GetInstancesWithCacheAsync(serviceName, _distributedCache, _cacheEntryOptions, null, cancellationToken);

        if (availableServiceInstances.Count == 0)
        {
            _logger.LogWarning("No service instances are available for '{serviceName}'.", serviceName);
            return requestUri;
        }

        int instanceIndex = await GetNextInstanceIndexAsync(serviceName, availableServiceInstances.Count, cancellationToken);
        IServiceInstance serviceInstance = availableServiceInstances[instanceIndex];

        _logger.LogDebug("Resolved '{serviceName}' to '{serviceInstance}'.", serviceName, serviceInstance.Uri);
        return new Uri(serviceInstance.Uri, requestUri.PathAndQuery);
    }

    private async Task<int> GetNextInstanceIndexAsync(string serviceName, int instanceCount, CancellationToken cancellationToken)
    {
        string cacheKey = $"{CacheKeyPrefix}{serviceName}";

        if (_distributedCache == null)
        {
            return _lastUsedIndexPerServiceName.AddOrUpdate(cacheKey, _ => CalculateNextIndex(null, instanceCount),
                (_, lastUsedIndex) => CalculateNextIndex(lastUsedIndex, instanceCount));
        }

        // IDistributed cache does not provide an atomic increment operation, so this is best-effort.
        byte[]? cacheEntry = await _distributedCache.GetAsync(cacheKey, cancellationToken);
        int? lastUsedIndex = cacheEntry is { Length: > 0 } ? BitConverter.ToInt16(cacheEntry) : null;

        int instanceIndex = CalculateNextIndex(lastUsedIndex, instanceCount);
        await _distributedCache.SetAsync(cacheKey, BitConverter.GetBytes(instanceIndex), _cacheEntryOptions, cancellationToken);
        return instanceIndex;
    }

    private static int CalculateNextIndex(int? lastUsedIndex, int instanceCount)
    {
        if (lastUsedIndex == null)
        {
            return 0;
        }

        int instanceIndex = lastUsedIndex.Value + 1;

        if (instanceIndex >= instanceCount)
        {
            instanceIndex = 0;
        }

        return instanceIndex;
    }

    /// <inheritdoc />
    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
