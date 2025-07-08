// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
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

        HealthCheckResult? result = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);

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

        HealthCheckResult? result = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeHealthStatus_ReturnsExpected()
    {
        (EurekaServerHealthContributor contributor, _) = CreateHealthContributor();

        contributor.MakeHealthStatus(InstanceStatus.Down).Should().Be(HealthStatus.Down);
        contributor.MakeHealthStatus(InstanceStatus.Up).Should().Be(HealthStatus.Up);
        contributor.MakeHealthStatus(InstanceStatus.Starting).Should().Be(HealthStatus.Unknown);
        contributor.MakeHealthStatus(InstanceStatus.Unknown).Should().Be(HealthStatus.Unknown);
        contributor.MakeHealthStatus(InstanceStatus.OutOfService).Should().Be(HealthStatus.OutOfService);
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

        Dictionary<string, int> appsDictionary =
            result.Details.Should().ContainKey("applications").WhoseValue.Should().BeOfType<Dictionary<string, int>>().Subject;

        appsDictionary.Should().HaveCount(2);
        appsDictionary.Should().ContainKey("app1").WhoseValue.Should().Be(2);
        appsDictionary.Should().ContainKey("app2").WhoseValue.Should().Be(2);
    }

    [Fact]
    public void AddFetchStatus_AddsExpected()
    {
        (EurekaServerHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();

        var results = new HealthCheckResult();
        contributor.AddFetchStatus(results, null);

        results.Details.Should().ContainKey("fetchStatus").WhoseValue.Should().Be("Not fetching");

        results = new HealthCheckResult();
        clientOptions.ShouldFetchRegistry = true;
        contributor.AddFetchStatus(results, null);

        results.Details.Should().ContainKey("fetch").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("Not yet successfully connected");
        results.Details.Should().ContainKey("fetchTime").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("UNKNOWN");
        results.Details.Should().ContainKey("fetchStatus").WhoseValue.Should().Be("UNKNOWN");

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * clientOptions.RegistryFetchIntervalSeconds * 10;
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);
        contributor.AddFetchStatus(results, dateTime);

        results.Details.Should().ContainKey("fetch").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("Reporting failures");
        results.Details.Should().ContainKey("fetchTime").WhoseValue.Should().Be(dateTime.ToString("s", CultureInfo.InvariantCulture));
        results.Details.Should().ContainKey("fetchFailures").WhoseValue.Should().BeOfType<long>().Which.Should().Be(10);
        results.Details.Should().ContainKey("fetchStatus").WhoseValue.Should().Be("DOWN");
    }

    [Fact]
    public void AddHeartbeatStatus_AddsExpected()
    {
        (EurekaServerHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();
        var results = new HealthCheckResult();
        contributor.AddHeartbeatStatus(results, null);

        results.Details.Should().ContainKey("heartbeatStatus").WhoseValue.Should().Be("Not registering");

        results = new HealthCheckResult();

        clientOptions.ShouldRegisterWithEureka = true;

        var instanceOptions = new EurekaInstanceOptions();
        contributor.AddHeartbeatStatus(results, null);

        results.Details.Should().ContainKey("heartbeat").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("Not yet successfully connected");
        results.Details.Should().ContainKey("heartbeatTime").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("UNKNOWN");
        results.Details.Should().ContainKey("heartbeatStatus").WhoseValue.Should().Be("UNKNOWN");

        results = new HealthCheckResult();
        long ticks = DateTime.UtcNow.Ticks - TimeSpan.TicksPerSecond * instanceOptions.LeaseRenewalIntervalInSeconds * 10;
        var dateTime = new DateTime(ticks, DateTimeKind.Utc);
        contributor.AddHeartbeatStatus(results, dateTime);

        results.Details.Should().ContainKey("heartbeat").WhoseValue.Should().BeOfType<string>().Which.Should().Contain("Reporting failures");
        results.Details.Should().ContainKey("heartbeatTime").WhoseValue.Should().Be(dateTime.ToString("s", CultureInfo.InvariantCulture));
        results.Details.Should().ContainKey("heartbeatFailures").WhoseValue.Should().BeOfType<long>().Which.Should().Be(10);
        results.Details.Should().ContainKey("heartbeatStatus").WhoseValue.Should().Be("DOWN");
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
