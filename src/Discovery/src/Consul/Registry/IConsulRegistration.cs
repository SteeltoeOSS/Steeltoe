// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Consul.Registry;

public interface IConsulRegistration : IServiceInstance
{
    /// <summary>
    /// Gets the Consul service registration.
    /// </summary>
    AgentServiceRegistration Service { get; }

    /// <summary>
    /// Gets the instance id to use for registration.
    /// </summary>
    string InstanceId { get; }
}
