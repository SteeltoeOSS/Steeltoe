// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class EurekaApplicationsHealthContributor : IHealthContributor
{
    private readonly EurekaDiscoveryClient _discoveryClient;
    private readonly ILogger<EurekaApplicationsHealthContributor> _logger;

    public string Id { get; } = "eurekaApplications";

    public EurekaApplicationsHealthContributor(EurekaDiscoveryClient discoveryClient, ILogger<EurekaApplicationsHealthContributor> logger = null)
    {
        _discoveryClient = discoveryClient;
        _logger = logger;
    }

    // Testing
    internal EurekaApplicationsHealthContributor()
    {
    }

    public HealthCheckResult Health()
    {
        var result = new HealthCheckResult
        {
            Status = HealthStatus.Up,
            Description = "No monitored applications"
        };

        IList<string> appNames = GetMonitoredApplications(_discoveryClient.ClientConfig);

        foreach (string appName in appNames)
        {
            Application app = _discoveryClient.GetApplication(appName);
            AddApplicationHealthStatus(appName, app, result);
        }

        result.Description = result.Status != HealthStatus.Up
            ? "At least one monitored application has no instances UP"
            : "All monitored applications have at least one instance UP";

        result.Details.Add("status", result.Status.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
        result.Details.Add("statusDescription", result.Description);
        return result;
    }

    internal void AddApplicationHealthStatus(string appName, Application app, HealthCheckResult result)
    {
        if (app != null && app.Name == appName)
        {
            int upCount = app.Instances.Count(x => x.Status == InstanceStatus.Up);

            if (upCount <= 0)
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

    internal IList<string> GetMonitoredApplications(IEurekaClientConfig clientConfig)
    {
        IList<string> configApps = GetApplicationsFromConfig(clientConfig);

        if (configApps != null)
        {
            return configApps;
        }

        IList<Application> regApps = _discoveryClient.Applications.GetRegisteredApplications();
        return regApps.Select(app => app.Name).ToList();
    }

    internal IList<string> GetApplicationsFromConfig(IEurekaClientConfig clientConfig)
    {
        if (clientConfig is EurekaClientConfig config)
        {
            string[] monitoredApps = config.HealthMonitoredApps?.Split(new[]
            {
                ','
            }, StringSplitOptions.RemoveEmptyEntries);

            if (monitoredApps != null && monitoredApps.Length > 0)
            {
                var results = new List<string>();

                foreach (string str in monitoredApps)
                {
                    results.Add(str.Trim());
                }

                return results;
            }
        }

        return null;
    }
}
