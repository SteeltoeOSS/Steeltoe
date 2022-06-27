// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Discovery;

/// <summary>
/// Represents a Service registry (e.g. Consul, etc.)
/// </summary>
/// <typeparam name="T">a type representing a service instance.</typeparam>
public interface IServiceRegistry<in T> : IDisposable
    where T : IServiceInstance
{
    /// <summary>
    /// Register a service instance in the service registry.
    /// </summary>
    /// <param name="registration">the service instance to register.</param>
    void Register(T registration);

    /// <summary>
    /// Deregister a service instance in the service registry.
    /// </summary>
    /// <param name="registration">the service instance to register.</param>
    void Deregister(T registration);

    /// <summary>
    /// Update the registration in the service registry with the provided status.
    /// </summary>
    /// <param name="registration">the registration to update.</param>
    /// <param name="status">the status.</param>
    void SetStatus(T registration, string status);

    /// <summary>
    /// Return the current status of the service registry registration.
    /// </summary>
    /// <typeparam name="S">the status.</typeparam>
    /// <param name="registration">the service registration to obtain status for.</param>
    /// <returns>the returned status.</returns>
    S GetStatus<S>(T registration)
        where S : class;
}
