// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Consul.Discovery;

public interface IConsulDiscoveryClient : IDiscoveryClient, IDisposable
{
    /// <summary>
    /// Gets all service instances for the given service ID.
    /// </summary>
    /// <param name="serviceId">
    /// ID of the service to lookup.
    /// </param>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of found service instances.
    /// </returns>
    Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, QueryOptions queryOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all service instances from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of found service instances.
    /// </returns>
    Task<IList<IServiceInstance>> GetAllInstancesAsync(QueryOptions queryOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all service names from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of found service names.
    /// </returns>
    Task<IList<string>> GetServiceNamesAsync(QueryOptions queryOptions, CancellationToken cancellationToken);
}
