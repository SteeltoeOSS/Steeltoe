// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.HttpClients.LoadBalancers;

/// <summary>
/// Returns random service instances.
/// </summary>
public sealed class RandomLoadBalancer : ILoadBalancer
{
    private readonly ServiceInstancesResolver _serviceInstancesResolver;
    private readonly ILogger<RandomLoadBalancer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomLoadBalancer" /> class.
    /// </summary>
    /// <param name="serviceInstancesResolver">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public RandomLoadBalancer(ServiceInstancesResolver serviceInstancesResolver, ILogger<RandomLoadBalancer> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceInstancesResolver);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceInstancesResolver = serviceInstancesResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        string serviceName = requestUri.Host;
        _logger.LogTrace("Resolving service instance for '{ServiceName}'.", serviceName);

        IList<IServiceInstance> availableServiceInstances = await _serviceInstancesResolver.ResolveInstancesAsync(serviceName, cancellationToken);

        if (availableServiceInstances.Count == 0)
        {
            _logger.LogWarning("No service instances are available for '{ServiceName}'.", serviceName);
            return requestUri;
        }

        // Load balancer instance selection predictability is not likely to be a security concern.
        int index = Random.Shared.Next(availableServiceInstances.Count);
        IServiceInstance serviceInstance = availableServiceInstances[index];

        _logger.LogDebug("Resolved '{ServiceName}' to '{ServiceInstance}'.", serviceName, serviceInstance.Uri);
        return new Uri(serviceInstance.Uri, requestUri.PathAndQuery);
    }

    /// <inheritdoc />
    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
