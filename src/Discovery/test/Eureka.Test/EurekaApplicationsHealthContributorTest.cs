// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Health;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaApplicationsHealthContributorTest
{
    [Fact]
    public async Task CheckHealthAsync_EurekaDisabled()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false"
        };

        (EurekaApplicationsHealthContributor contributor, _) = CreateHealthContributor(appSettings);

        HealthCheckResult? result = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public void GetApplicationsFromConfiguration_ReturnsExpected()
    {
        (EurekaApplicationsHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();

        IList<string>? apps = contributor.GetApplicationsFromConfiguration();

        apps.Should().BeNull();

        clientOptions.Health.MonitoredApps = "foo,bar, boo ";

        apps = contributor.GetApplicationsFromConfiguration();

        apps.Should().HaveCount(3);
        apps.Should().Contain("foo");
        apps.Should().Contain("bar");
        apps.Should().Contain("boo");
    }

    [Fact]
    public void AddApplicationHealthStatus_AddsExpected()
    {
        (EurekaApplicationsHealthContributor contributor, _) = CreateHealthContributor();

        var app1 = new ApplicationInfo("app1", [
            new InstanceInfoBuilder().WithId("id1").WithStatus(InstanceStatus.Up).Build(),
            new InstanceInfoBuilder().WithId("id2").WithStatus(InstanceStatus.Up).Build()
        ]);

        var app2 = new ApplicationInfo("app2", [
            new InstanceInfoBuilder().WithId("id1").WithStatus(InstanceStatus.Down).Build(),
            new InstanceInfoBuilder().WithId("id2").WithStatus(InstanceStatus.Starting).Build()
        ]);

        var result = new HealthCheckResult();
        contributor.AddApplicationHealthStatus("foobar", app1, result);

        result.Status.Should().Be(HealthStatus.Down);
        result.Details.Should().ContainKey("foobar").WhoseValue.Should().Be("No instances found");

        result = new HealthCheckResult
        {
            Status = HealthStatus.Up
        };

        contributor.AddApplicationHealthStatus("app1", app1, result);

        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().ContainKey("app1").WhoseValue.Should().Be("2 instances with UP status");

        result = new HealthCheckResult
        {
            Status = HealthStatus.Up
        };

        contributor.AddApplicationHealthStatus("app2", app2, result);

        result.Status.Should().Be(HealthStatus.Down);
        result.Details.Should().ContainKey("app2").WhoseValue.Should().Be("0 instances with UP status");
    }

    private static (EurekaApplicationsHealthContributor Contributor, EurekaClientOptions ClientOptions) CreateHealthContributor(
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

        services.AddHealthContributor<EurekaApplicationsHealthContributor>();
        services.AddEurekaDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        EurekaApplicationsHealthContributor contributor =
            serviceProvider.GetServices<IHealthContributor>().OfType<EurekaApplicationsHealthContributor>().Single();

        var clientOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();

        return (contributor, clientOptionsMonitor.CurrentValue);
    }
}
