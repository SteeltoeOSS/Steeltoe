// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Holds settings used to configure service discovery with the Spring Cloud Config Server provider.
/// </summary>
public sealed class ConfigServerDiscoveryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the discovery-first feature is enabled. Default value: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Service ID of the Config Server to use during discovery-first. Default value: "configserver".
    /// </summary>
    public string? ServiceId { get; set; } = "configserver";
}
