// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaServerHealthContributorTest
{
    [Fact]
    public void MakeHealthStatus_ReturnsExpected()
    {
        var contrib = new EurekaServerHealthContributor();
        Assert.Equal(HealthStatus.Down, contrib.MakeHealthStatus(InstanceStatus.Down));
        Assert.Equal(HealthStatus.Up, contrib.MakeHealthStatus(InstanceStatus.Up));
        Assert.Equal(HealthStatus.Unknown, contrib.MakeHealthStatus(InstanceStatus.Starting));
        Assert.Equal(HealthStatus.Unknown, contrib.MakeHealthStatus(InstanceStatus.Unknown));
        Assert.Equal(HealthStatus.OutOfService, contrib.MakeHealthStatus(InstanceStatus.OutOfService));
    }

    [Fact]
    public void AddApplications_AddsExpected()
    {
        var contrib = new EurekaServerHealthContributor();
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications(new List<Application>
        {
            app1,
            app2
        });

        var result = new HealthCheckResult();
        contrib.AddApplications(apps, result);
        Dictionary<string, object> details = result.Details;
        Assert.Contains("applications", details.Keys);
        var appsDict = details["applications"] as Dictionary<string, int>;
        Assert.Contains("app1", appsDict.Keys);
        Assert.Contains("app2", appsDict.Keys);
        Assert.Equal(2, appsDict.Keys.Count);
        int count1 = appsDict["app1"];
        Assert.Equal(2, count1);
        int count2 = appsDict["app2"];
        Assert.Equal(2, count2);
    }

    [Fact]
    public void AddFetchStatus_AddsExpected()
    {
        var contrib = new EurekaServerHealthContributor();
        var results = new HealthCheckResult();
        contrib.AddFetchStatus(null, results, 0);
        Assert.Contains("fetchStatus", results.Details.Keys);
        Assert.Equal("Not fetching", results.Details["fetchStatus"]);

        results = new HealthCheckResult();

        var configuration = new EurekaClientConfiguration
        {
            ShouldFetchRegistry = true
        };

        contrib.AddFetchStatus(configuration, results, 0);
        Assert.Contains("fetch", results.Details.Keys);
        Assert.Contains("Not yet successfully connected", (string)results.Details["fetch"], StringComparison.Ordinal);
        Assert.Contains("fetchTime", results.Details.Keys);
        Assert.Contains("UNKNOWN", (string)results.Details["fetchTime"], StringComparison.Ordinal);
        Assert.Contains("fetchStatus", results.Details.Keys);
        Assert.Equal("UNKNOWN", results.Details["fetchStatus"]);

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * configuration.RegistryFetchIntervalSeconds * 10;
        var dateTime = new DateTime(ticks);
        contrib.AddFetchStatus(configuration, results, ticks);
        Assert.Contains("fetch", results.Details.Keys);
        Assert.Contains("Reporting failures", (string)results.Details["fetch"], StringComparison.Ordinal);
        Assert.Contains("fetchTime", results.Details.Keys);
        Assert.Equal(dateTime.ToString("s", CultureInfo.InvariantCulture), (string)results.Details["fetchTime"]);
        Assert.Contains("fetchFailures", results.Details.Keys);
        Assert.Equal(10, (long)results.Details["fetchFailures"]);
        Assert.Contains("fetchStatus", results.Details.Keys);
        Assert.Equal("DOWN", results.Details["fetchStatus"]);
    }

    [Fact]
    public void AddHeartbeatStatus_AddsExpected()
    {
        var contrib = new EurekaServerHealthContributor();
        var results = new HealthCheckResult();
        contrib.AddHeartbeatStatus(null, null, results, 0);
        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("Not registering", results.Details["heartbeatStatus"]);

        results = new HealthCheckResult();

        var clientConfig = new EurekaClientConfiguration
        {
            ShouldRegisterWithEureka = true
        };

        var instanceConfig = new EurekaInstanceConfiguration();

        contrib.AddHeartbeatStatus(clientConfig, instanceConfig, results, 0);
        Assert.Contains("heartbeat", results.Details.Keys);
        Assert.Contains("Not yet successfully connected", (string)results.Details["heartbeat"], StringComparison.Ordinal);
        Assert.Contains("heartbeatTime", results.Details.Keys);
        Assert.Contains("UNKNOWN", (string)results.Details["heartbeatTime"], StringComparison.Ordinal);
        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("UNKNOWN", results.Details["heartbeatStatus"]);

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * instanceConfig.LeaseRenewalIntervalInSeconds * 10;
        var dateTime = new DateTime(ticks);
        contrib.AddHeartbeatStatus(clientConfig, instanceConfig, results, ticks);
        Assert.Contains("heartbeat", results.Details.Keys);
        Assert.Contains("Reporting failures", (string)results.Details["heartbeat"], StringComparison.Ordinal);
        Assert.Contains("heartbeatTime", results.Details.Keys);
        Assert.Equal(dateTime.ToString("s", CultureInfo.InvariantCulture), (string)results.Details["heartbeatTime"]);
        Assert.Contains("heartbeatFailures", results.Details.Keys);
        Assert.Equal(10, (long)results.Details["heartbeatFailures"]);
        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("DOWN", results.Details["heartbeatStatus"]);
    }
}
