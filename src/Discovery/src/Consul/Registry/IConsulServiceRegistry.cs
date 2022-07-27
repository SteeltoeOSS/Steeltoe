// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// A Consul service registry
/// </summary>
public interface IConsulServiceRegistry : IServiceRegistry<IConsulRegistration>
{
    /// <summary>
    /// Register the provided registration in Consul
    /// </summary>
    /// <param name="registration">the registration to register</param>
    /// <returns>the task</returns>
    Task RegisterAsync(IConsulRegistration registration);

    /// <summary>
    /// Deregister the provided registration in Consul
    /// </summary>
    /// <param name="registration">the registration to register</param>
    /// <returns>the task</returns>
    Task DeregisterAsync(IConsulRegistration registration);

    /// <summary>
    /// Set the status of the registration in Consul
    /// </summary>
    /// <param name="registration">the registration to register</param>
    /// <param name="status">the status value to set</param>
    /// <returns>the task</returns>
    Task SetStatusAsync(IConsulRegistration registration, string status);

    /// <summary>
    /// Get the status of the registration in Consul
    /// </summary>
    /// <param name="registration">the registration to register</param>
    /// <returns>the status value</returns>
    Task<object> GetStatusAsync(IConsulRegistration registration);
}