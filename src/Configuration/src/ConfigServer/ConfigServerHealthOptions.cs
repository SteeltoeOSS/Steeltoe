// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Holds settings used to configure health checks with the Spring Cloud Config Server provider.
/// </summary>
public sealed class ConfigServerHealthOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether health checks are enabled. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the health check cache time-to-live (in milliseconds). Default value: 300_000 (5 minutes).
    /// </summary>
    public long TimeToLive { get; set; } = 300_000;
}
