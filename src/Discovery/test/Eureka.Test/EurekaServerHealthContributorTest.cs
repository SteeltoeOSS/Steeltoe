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

        await using ServiceProvider serviceProvider = BuildServiceProvider(appSettings);
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);

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

        await using ServiceProvider serviceProvider = BuildServiceProvider(appSettings);
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);

        HealthCheckResult? result = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public void MakeHealthStatus_ReturnsExpected()
    {
        using ServiceProvider serviceProvider = BuildServiceProvider();
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);

        contributor.MakeHealthStatus(InstanceStatus.Down).Should().Be(HealthStatus.Down);
        contributor.MakeHealthStatus(InstanceStatus.Up).Should().Be(HealthStatus.Up);
        contributor.MakeHealthStatus(InstanceStatus.Starting).Should().Be(HealthStatus.Unknown);
        contributor.MakeHealthStatus(InstanceStatus.Unknown).Should().Be(HealthStatus.Unknown);
        contributor.MakeHealthStatus(InstanceStatus.OutOfService).Should().Be(HealthStatus.OutOfService);
    }

    [Fact]
    public void AddApplications_AddsExpected()
    {
        using ServiceProvider serviceProvider = BuildServiceProvider();
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);

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
        using ServiceProvider serviceProvider = BuildServiceProvider();
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);
        EurekaClientOptions clientOptions = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>().CurrentValue;

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
        using ServiceProvider serviceProvider = BuildServiceProvider();
        EurekaServerHealthContributor contributor = GetContributor(serviceProvider);
        EurekaClientOptions clientOptions = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>().CurrentValue;

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

    private static ServiceProvider BuildServiceProvider(IDictionary<string, string?>? appSettings = null)
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (appSettings != null)
        {
            configurationBuilder.AddInMemoryCollection(appSettings);
        }

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);

        services.AddOptions<EurekaClientOptions>().Configure(options =>
        {
            options.ShouldFetchRegistry = false;
            options.ShouldRegisterWithEureka = false;
        });

        services.AddEurekaDiscoveryClient();

        return services.BuildServiceProvider(true);
    }

    private static EurekaServerHealthContributor GetContributor(ServiceProvider serviceProvider)
    {
        return serviceProvider.GetServices<IHealthContributor>().OfType<EurekaServerHealthContributor>().Single();
    }
}
