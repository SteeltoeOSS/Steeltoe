// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Discovery.Consul.Registry;

public interface IServiceRegistrar : IDisposable
{
    /// <summary>
    /// Start the service registrar.
    /// </summary>
    void Start();

    /// <summary>
    /// Register any registrations configured.
    /// </summary>
    void Register();

    /// <summary>
    /// Deregister any registrations configured.
    /// </summary>
    void Deregister();
}
