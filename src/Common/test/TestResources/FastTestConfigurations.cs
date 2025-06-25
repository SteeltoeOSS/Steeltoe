// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Specifies settings to speed up running tests that don't need the full behavior.
/// </summary>
[Flags]
public enum FastTestConfigurations
{
    /// <summary>
    /// Do not connect to Config Server to fetch configuration.
    /// </summary>
    ConfigServer = 1,

    /// <summary>
    /// Do not connect to Eureka/Consul to register the local service instance.
    /// </summary>
    Discovery = 2,

    /// <summary>
    /// Use connection strings that fail fast, without relying on a network timeout.
    /// </summary>
    Connectors = 4,

    /// <summary>
    /// Enables all available options.
    /// </summary>
    All = ConfigServer | Discovery | Connectors
}
