// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Discovery;

/// <summary>
/// Provides access to remote service instances using a discovery server.
/// </summary>
public interface IDiscoveryClient
{
    /// <summary>
    /// Gets a human-readable description of this discovery client.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets information used to register the local service instance (this app) to the discovery server.
    /// </summary>
    /// <returns>
    /// The service instance that represents this app, or <c>null</c> when unavailable.
    /// </returns>
    IServiceInstance? GetLocalServiceInstance();

    /// <summary>
    /// Gets all registered service IDs from the discovery server.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of service IDs.
    /// </returns>
    Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all service instances associated with the specified service ID from the discovery server.
    /// </summary>
    /// <param name="serviceId">
    /// The ID of the service to lookup.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of remote service instances.
    /// </returns>
    Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken);

    /// <summary>
    /// Deregisters the local service (this app) from the discovery server.
    /// </summary>
    /// <remarks>
    /// This method exists for two reasons, instead of just reusing <see cref="IAsyncDisposable" />. The first reason is that this method enables
    /// cancellation. The second reason is more complicated. Deregistration typically requires to send an HTTP request to the discovery server. When using
    /// `IHttpClientFactory`, it requires the IoC container to obtain an <see cref="HttpClient" />. But that fails when the container is being disposed.
    /// Deregistration must be performed earlier. That's why implementations should register `DiscoveryClientHostedService` in the IoC container, which is a
    /// hosted service that performs deregistration (by calling this method) when the app is terminating. At that time, the IoC container is still
    /// accessible.
    /// </remarks>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    Task ShutdownAsync(CancellationToken cancellationToken);
}
