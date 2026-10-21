// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Discovery;

/// <summary>
/// Provides access to the status of the local service instance (this app) in a discovery server. Implemented by discovery clients that register the
/// local service instance, so that it can be taken in and out of service at runtime, for example during blue/green deployments.
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Gets a value indicating whether the local service instance (this app) is registered with the discovery server, based on the current
    /// configuration.
    /// </summary>
    bool IsRegistrationEnabled { get; }

    /// <summary>
    /// Gets the status of the local service instance (this app) from the discovery server.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A JSON-serializable object that describes the status, whose shape depends on the discovery server.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The local service instance is not registered with the discovery server.
    /// </exception>
    Task<object> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Changes the status of the local service instance (this app) in the discovery server.
    /// </summary>
    /// <param name="status">
    /// The new status, for example: "UP" or "OUT_OF_SERVICE". The supported values depend on the discovery server.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The specified status is not supported by the discovery server.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The local service instance is not registered with the discovery server.
    /// </exception>
    Task SetStatusAsync(string status, CancellationToken cancellationToken);
}
