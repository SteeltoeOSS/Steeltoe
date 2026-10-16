// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// Determines how Consul learns about the health of this app.
/// </summary>
internal enum HealthCheckMode
{
    /// <summary>
    /// The service is registered without a check. Consul then always considers it healthy.
    /// </summary>
    None,

    /// <summary>
    /// A TTL check is registered, and this app periodically reports to Consul that it is alive.
    /// </summary>
    Ttl,

    /// <summary>
    /// An HTTP check is registered, and Consul periodically polls the health endpoint of this app.
    /// </summary>
    Http
}
