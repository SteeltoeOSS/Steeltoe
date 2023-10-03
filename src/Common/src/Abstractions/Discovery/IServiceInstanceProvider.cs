// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Discovery;

public interface IServiceInstanceProvider
{
    /// <summary>
    /// Gets a human-readable description of this provider.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets all known service IDs.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The service IDs.
    /// </returns>
    Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets all service instances associated with the specified service ID.
    /// </summary>
    /// <param name="serviceId">
    /// The ID of the service to lookup.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of service instances.
    /// </returns>
    Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken);
}
