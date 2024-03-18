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
    /// Gets information used to register the local service (this app) to the discovery server.
    /// </summary>
    /// <returns>
    /// The service instance that represents this app.
    /// </returns>
    IServiceInstance GetLocalServiceInstance();

    /// <summary>
    /// Gets all registered service IDs from the discovery server.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of service IDs.
    /// </returns>
    Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken);

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

    Task ShutdownAsync(CancellationToken cancellationToken);
}
