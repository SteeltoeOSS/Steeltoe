// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// A Consul service registry.
/// </summary>
public interface IConsulServiceRegistry : IDisposable
{
    /// <summary>
    /// Register a service instance in the service registry.
    /// </summary>
    /// <param name="registration">
    /// the service instance to register.
    /// </param>
    void Register(ConsulRegistration registration);

    /// <summary>
    /// Register the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// the registration to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// the task.
    /// </returns>
    Task RegisterAsync(ConsulRegistration registration, CancellationToken cancellationToken);

    /// <summary>
    /// Deregister a service instance in the service registry.
    /// </summary>
    /// <param name="registration">
    /// the service instance to register.
    /// </param>
    void Deregister(ConsulRegistration registration);

    /// <summary>
    /// Deregister the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// the registration to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// the task.
    /// </returns>
    Task DeregisterAsync(ConsulRegistration registration, CancellationToken cancellationToken);

    /// <summary>
    /// Update the registration in the service registry with the provided status.
    /// </summary>
    /// <param name="registration">
    /// the registration to update.
    /// </param>
    /// <param name="status">
    /// the status.
    /// </param>
    void SetStatus(ConsulRegistration registration, string status);

    /// <summary>
    /// Set the status of the registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// the registration to register.
    /// </param>
    /// <param name="status">
    /// the status value to set.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// the task.
    /// </returns>
    Task SetStatusAsync(ConsulRegistration registration, string status, CancellationToken cancellationToken);

    /// <summary>
    /// Return the current status of the service registry registration.
    /// </summary>
    /// <param name="registration">
    /// the service registration to obtain status for.
    /// </param>
    /// <returns>
    /// the returned status.
    /// </returns>
    string GetStatus(ConsulRegistration registration);

    /// <summary>
    /// Get the status of the registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// the registration to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// the status value.
    /// </returns>
    Task<string> GetStatusAsync(ConsulRegistration registration, CancellationToken cancellationToken);
}
