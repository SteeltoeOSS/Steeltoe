// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Discovery.Eureka.Configuration;

public sealed class EurekaHealthOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to activate an <see cref="IHealthContributor" /> that verifies connectivity to the Eureka server. Default
    /// value: true.
    /// </summary>
    [ConfigurationKeyName("Enabled")]
    public bool ContributorEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a comma-delimited list of applications in Eureka this app depends on. Leave empty for all apps. Their status is taken into account by
    /// <see cref="EurekaApplicationsHealthContributor" />, which requires manual addition to the IoC container.
    /// </summary>
    public string? MonitoredApps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to query ASP.NET Core health checks and <see cref="IHealthContributor" />s during registration and renewals,
    /// in order to determine the status of the running app to report back to Eureka. Default value: true.
    /// </summary>
    public bool CheckEnabled { get; set; }
}
