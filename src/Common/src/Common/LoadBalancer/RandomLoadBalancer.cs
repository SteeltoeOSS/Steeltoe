// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Common.LoadBalancer;

/// <summary>
/// Returns random service instances, optionally using distributed caching for service lookups.
/// </summary>
public sealed class RandomLoadBalancer : ILoadBalancer
{
    private readonly IDiscoveryClient _discoveryClient;
    private readonly IDistributedCache? _distributedCache;
    private readonly DistributedCacheEntryOptions? _cacheEntryOptions;
    private readonly ILogger<RandomLoadBalancer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomLoadBalancer" /> class.
    /// </summary>
    /// <param name="discoveryClient">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public RandomLoadBalancer(IDiscoveryClient discoveryClient, ILogger<RandomLoadBalancer> logger)
        : this(discoveryClient, null, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomLoadBalancer" /> class.
    /// </summary>
    /// <param name="discoveryClient">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="distributedCache">
    /// For caching service instance data.
    /// </param>
    /// <param name="cacheEntryOptions">
    /// Configuration for <paramref name="distributedCache" />.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public RandomLoadBalancer(IDiscoveryClient discoveryClient, IDistributedCache? distributedCache, DistributedCacheEntryOptions? cacheEntryOptions,
        ILogger<RandomLoadBalancer> logger)
    {
        ArgumentGuard.NotNull(discoveryClient);
        ArgumentGuard.NotNull(logger);

        _discoveryClient = discoveryClient;
        _distributedCache = distributedCache;
        _cacheEntryOptions = cacheEntryOptions;
        _logger = logger;
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

        // Load balancer instance selection predictability is not likely to be a security concern.
        int index = Random.Shared.Next(availableServiceInstances.Count);
        IServiceInstance serviceInstance = availableServiceInstances[index];

        _logger.LogDebug("Resolved '{serviceName}' to '{serviceInstance}'.", serviceName, serviceInstance.Uri);
        return new Uri(serviceInstance.Uri, requestUri.PathAndQuery);
    }

    /// <inheritdoc />
    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
