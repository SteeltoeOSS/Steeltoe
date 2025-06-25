// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Reports whether the configured list of apps this app depends on are reachable.
/// </summary>
public sealed class EurekaApplicationsHealthContributor : IHealthContributor
{
    private readonly EurekaDiscoveryClient _discoveryClient;
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;

    public string Id => "eurekaApplications";

    public EurekaApplicationsHealthContributor(EurekaDiscoveryClient discoveryClient, IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(discoveryClient);
        ArgumentNullException.ThrowIfNull(clientOptionsMonitor);

        _discoveryClient = discoveryClient;
        _clientOptionsMonitor = clientOptionsMonitor;
    }

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_clientOptionsMonitor.CurrentValue.Enabled)
        {
            return Task.FromResult<HealthCheckResult?>(null);
        }

        var result = new HealthCheckResult
        {
            Status = HealthStatus.Up,
            Description = "No monitored applications"
        };

        IList<string> appNames = GetMonitoredApplications();

        foreach (string appName in appNames)
        {
            ApplicationInfo? app = _discoveryClient.GetApplication(appName);
            AddApplicationHealthStatus(appName, app, result);
        }

        result.Description = result.Status != HealthStatus.Up
            ? "At least one monitored application has no instances UP"
            : "All monitored applications have at least one instance UP";

        result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
        result.Details.Add("statusDescription", result.Description);

        return Task.FromResult<HealthCheckResult?>(result);
    }

    internal void AddApplicationHealthStatus(string appName, ApplicationInfo? app, HealthCheckResult result)
    {
        if (app != null && app.Name == appName)
        {
            int upCount = app.Instances.Count(instance => instance.EffectiveStatus == InstanceStatus.Up);

            if (upCount == 0)
            {
                result.Status = HealthStatus.Down;
            }

            result.Details[appName] = $"{upCount} instances with UP status";
        }
        else
        {
            result.Status = HealthStatus.Down;
            result.Details[appName] = "No instances found";
        }
    }

    private IList<string> GetMonitoredApplications()
    {
        IList<string>? configuredApplications = GetApplicationsFromConfiguration();

        if (configuredApplications != null)
        {
            return configuredApplications;
        }

        return _discoveryClient.Applications.Select(app => app.Name).ToArray();
    }

    internal IList<string>? GetApplicationsFromConfiguration()
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        string[]? monitoredApps = clientOptions.Health.MonitoredApps?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (monitoredApps is { Length: > 0 })
        {
            return monitoredApps.ToArray();
        }

        return null;
    }
}
