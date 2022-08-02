// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A Consul Discovery client.
/// </summary>
public interface IConsulDiscoveryClient : IDiscoveryClient, IDisposable
{
    /// <summary>
    /// Get all the instances for the given service ID.
    /// </summary>
    /// <param name="serviceId">
    /// the service to lookup.
    /// </param>
    /// <param name="queryOptions">
    /// any Consul query options to use.
    /// </param>
    /// <returns>
    /// list of found service instances.
    /// </returns>
    IList<IServiceInstance> GetInstances(string serviceId, QueryOptions queryOptions = null);

    /// <summary>
    /// Get all the instances from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// any Consul query options to use.
    /// </param>
    /// <returns>
    /// list of found service instances.
    /// </returns>
    IList<IServiceInstance> GetAllInstances(QueryOptions queryOptions = null);

    /// <summary>
    /// Get all of the services from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// any Consul query options to use.
    /// </param>
    /// <returns>
    /// list of found services.
    /// </returns>
    IList<string> GetServices(QueryOptions queryOptions = null);
}
