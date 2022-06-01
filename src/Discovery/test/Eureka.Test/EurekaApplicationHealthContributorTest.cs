// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaApplicationHealthContributorTest
{
    [Fact]
    public void GetApplicationsFromConfig_ReturnsExpected()
    {
        var contrib = new EurekaApplicationsHealthContributor();
        var config = new EurekaClientConfig();
        var apps = contrib.GetApplicationsFromConfig(config);
        Assert.Null(apps);
        config = new EurekaClientConfig
        {
            HealthMonitoredApps = "foo,bar, boo "
        };

        apps = contrib.GetApplicationsFromConfig(config);
        Assert.NotEmpty(apps);
        Assert.Equal(3, apps.Count);
        Assert.Contains("foo", apps);
        Assert.Contains("bar", apps);
        Assert.Contains("boo", apps);
    }

    [Fact]
    public void AddApplicationHealthStatus_AddsExpected()
    {
        var contrib = new EurekaApplicationsHealthContributor();
        var app1 = new Application("app1");
        app1.Add(new InstanceInfo { InstanceId = "id1", Status = InstanceStatus.UP });
        app1.Add(new InstanceInfo { InstanceId = "id2", Status = InstanceStatus.UP });

        var app2 = new Application("app2");
        app2.Add(new InstanceInfo { InstanceId = "id1", Status = InstanceStatus.DOWN });
        app2.Add(new InstanceInfo { InstanceId = "id2", Status = InstanceStatus.STARTING });

        var result = new HealthCheckResult();
        contrib.AddApplicationHealthStatus("app1", null, result);
        Assert.Equal(HealthStatus.DOWN, result.Status);
        Assert.Equal("No instances found", result.Details["app1"]);

        result = new HealthCheckResult();
        contrib.AddApplicationHealthStatus("foobar", app1, result);
        Assert.Equal(HealthStatus.DOWN, result.Status);
        Assert.Equal("No instances found", result.Details["foobar"]);

        result = new HealthCheckResult
        {
            Status = HealthStatus.UP
        };
        contrib.AddApplicationHealthStatus("app1", app1, result);
        Assert.Equal(HealthStatus.UP, result.Status);
        Assert.Equal("2 instances with UP status", result.Details["app1"]);

        result = new HealthCheckResult
        {
            Status = HealthStatus.UP
        };
        contrib.AddApplicationHealthStatus("app2", app2, result);
        Assert.Equal(HealthStatus.DOWN, result.Status);
        Assert.Equal("0 instances with UP status", result.Details["app2"]);
    }
}
