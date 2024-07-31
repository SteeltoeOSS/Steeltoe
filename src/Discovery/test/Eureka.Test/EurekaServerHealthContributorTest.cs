// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaServerHealthContributorTest
{
    [Fact]
    public async Task CheckHealthAsync_EurekaDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false"
        };

        (EurekaServerHealthContributor contributor, _) = CreateHealthContributor(appSettings);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_ContributorDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:health:enabled"] = "false"
        };

        (EurekaServerHealthContributor contributor, _) = CreateHealthContributor(appSettings);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeHealthStatus_ReturnsExpected()
    {
        (EurekaServerHealthContributor contributor, _) = CreateHealthContributor();

        Assert.Equal(HealthStatus.Down, contributor.MakeHealthStatus(InstanceStatus.Down));
        Assert.Equal(HealthStatus.Up, contributor.MakeHealthStatus(InstanceStatus.Up));
        Assert.Equal(HealthStatus.Unknown, contributor.MakeHealthStatus(InstanceStatus.Starting));
        Assert.Equal(HealthStatus.Unknown, contributor.MakeHealthStatus(InstanceStatus.Unknown));
        Assert.Equal(HealthStatus.OutOfService, contributor.MakeHealthStatus(InstanceStatus.OutOfService));
    }

    [Fact]
    public void AddApplications_AddsExpected()
    {
        (EurekaServerHealthContributor contributor, _) = CreateHealthContributor();

        var apps = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ])
        ]);

        var result = new HealthCheckResult();
        contributor.AddApplications(apps, result);

        Dictionary<string, object>? details = result.Details;
        Assert.Contains("applications", details.Keys);
        var appsDict = (Dictionary<string, int>)details["applications"];
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
        (EurekaServerHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();

        var results = new HealthCheckResult();
        contributor.AddFetchStatus(results, null);

        Assert.Contains("fetchStatus", results.Details.Keys);
        Assert.Equal("Not fetching", results.Details["fetchStatus"]);

        results = new HealthCheckResult();

        clientOptions.ShouldFetchRegistry = true;

        contributor.AddFetchStatus(results, null);

        Assert.Contains("fetch", results.Details.Keys);
        Assert.Contains("Not yet successfully connected", (string)results.Details["fetch"], StringComparison.Ordinal);
        Assert.Contains("fetchTime", results.Details.Keys);
        Assert.Contains("UNKNOWN", (string)results.Details["fetchTime"], StringComparison.Ordinal);
        Assert.Contains("fetchStatus", results.Details.Keys);
        Assert.Equal("UNKNOWN", results.Details["fetchStatus"]);

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * clientOptions.RegistryFetchIntervalSeconds * 10;
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);
        contributor.AddFetchStatus(results, dateTime);

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
        (EurekaServerHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();
        var results = new HealthCheckResult();
        contributor.AddHeartbeatStatus(results, null);

        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("Not registering", results.Details["heartbeatStatus"]);

        results = new HealthCheckResult();

        clientOptions.ShouldRegisterWithEureka = true;

        var instanceOptions = new EurekaInstanceOptions();
        contributor.AddHeartbeatStatus(results, null);

        Assert.Contains("heartbeat", results.Details.Keys);
        Assert.Contains("Not yet successfully connected", (string)results.Details["heartbeat"], StringComparison.Ordinal);
        Assert.Contains("heartbeatTime", results.Details.Keys);
        Assert.Contains("UNKNOWN", (string)results.Details["heartbeatTime"], StringComparison.Ordinal);
        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("UNKNOWN", results.Details["heartbeatStatus"]);

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * instanceOptions.LeaseRenewalIntervalInSeconds * 10;
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);
        contributor.AddHeartbeatStatus(results, dateTime);

        Assert.Contains("heartbeat", results.Details.Keys);
        Assert.Contains("Reporting failures", (string)results.Details["heartbeat"], StringComparison.Ordinal);
        Assert.Contains("heartbeatTime", results.Details.Keys);
        Assert.Equal(dateTime.ToString("s", CultureInfo.InvariantCulture), (string)results.Details["heartbeatTime"]);
        Assert.Contains("heartbeatFailures", results.Details.Keys);
        Assert.Equal(10, (long)results.Details["heartbeatFailures"]);
        Assert.Contains("heartbeatStatus", results.Details.Keys);
        Assert.Equal("DOWN", results.Details["heartbeatStatus"]);
    }

    private static (EurekaServerHealthContributor Contributor, EurekaClientOptions ClientOptions) CreateHealthContributor(
        IDictionary<string, string?>? appSettings = null)
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (appSettings != null)
        {
            configurationBuilder.AddInMemoryCollection(appSettings);
        }

        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();

        services.AddOptions<EurekaClientOptions>().Configure(options =>
        {
            options.ShouldFetchRegistry = false;
            options.ShouldRegisterWithEureka = false;
        });

        services.AddEurekaDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        EurekaServerHealthContributor contributor = serviceProvider.GetServices<IHealthContributor>().OfType<EurekaServerHealthContributor>().Single();
        var clientOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();

        return (contributor, clientOptionsMonitor.CurrentValue);
    }
}
