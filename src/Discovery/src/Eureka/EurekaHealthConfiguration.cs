// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaHealthConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable the management health contributor. Configuration property: eureka:client:health:enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of applications the management health contributor monitors. Configuration property: eureka:client:health:monitoredApps.
    /// </summary>
    public string? MonitoredApps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Eureka health check handler. Configuration property: eureka:client:health:checkEnabled.
    /// </summary>
    public bool CheckEnabled { get; set; } = true;
}
