// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaApplicationHealthContributorTest
{
    [Fact]
    public void GetApplicationsFromConfig_ReturnsExpected()
    {
        var contributor = new EurekaApplicationsHealthContributor();
        var clientOptions = new EurekaClientOptions();

        IList<string> apps = contributor.GetApplicationsFromConfig(clientOptions);
        Assert.Null(apps);

        clientOptions = new EurekaClientOptions
        {
            Health =
            {
                MonitoredApps = "foo,bar, boo "
            }
        };

        apps = contributor.GetApplicationsFromConfig(clientOptions);

        Assert.NotEmpty(apps);
        Assert.Equal(3, apps.Count);
        Assert.Contains("foo", apps);
        Assert.Contains("bar", apps);
        Assert.Contains("boo", apps);
    }

    [Fact]
    public void AddApplicationHealthStatus_AddsExpected()
    {
        var contributor = new EurekaApplicationsHealthContributor();
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1",
            Status = InstanceStatus.Up
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2",
            Status = InstanceStatus.Up
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1",
            Status = InstanceStatus.Down
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2",
            Status = InstanceStatus.Starting
        });

        var result = new HealthCheckResult();
        contributor.AddApplicationHealthStatus("app1", null, result);

        Assert.Equal(HealthStatus.Down, result.Status);
        Assert.Equal("No instances found", result.Details["app1"]);

        result = new HealthCheckResult();
        contributor.AddApplicationHealthStatus("foobar", app1, result);

        Assert.Equal(HealthStatus.Down, result.Status);
        Assert.Equal("No instances found", result.Details["foobar"]);

        result = new HealthCheckResult
        {
            Status = HealthStatus.Up
        };

        contributor.AddApplicationHealthStatus("app1", app1, result);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Equal("2 instances with UP status", result.Details["app1"]);

        result = new HealthCheckResult
        {
            Status = HealthStatus.Up
        };

        contributor.AddApplicationHealthStatus("app2", app2, result);

        Assert.Equal(HealthStatus.Down, result.Status);
        Assert.Equal("0 instances with UP status", result.Details["app2"]);
    }
}
