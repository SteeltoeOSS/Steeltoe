// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Management.Endpoint.Health;

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

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void GetApplicationsFromConfiguration_ReturnsExpected()
    {
        (EurekaApplicationsHealthContributor contributor, EurekaClientOptions clientOptions) = CreateHealthContributor();

        IList<string>? apps = contributor.GetApplicationsFromConfiguration();

        Assert.Null(apps);

        clientOptions.Health.MonitoredApps = "foo,bar, boo ";

        apps = contributor.GetApplicationsFromConfiguration();

        Assert.NotNull(apps);
        Assert.NotEmpty(apps);
        Assert.Equal(3, apps.Count);
        Assert.Contains("foo", apps);
        Assert.Contains("bar", apps);
        Assert.Contains("boo", apps);
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
        services.AddSingleton<IServer, TestServer>();

        services.AddOptions<EurekaClientOptions>().Configure(options =>
        {
            options.ShouldFetchRegistry = false;
            options.ShouldRegisterWithEureka = false;
        });

        services.AddHealthContributors([typeof(EurekaApplicationsHealthContributor)]);
        services.AddEurekaDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        EurekaApplicationsHealthContributor contributor =
            serviceProvider.GetRequiredService<IEnumerable<IHealthContributor>>().OfType<EurekaApplicationsHealthContributor>().Single();

        var clientOptionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();

        return (contributor, clientOptionsMonitor.CurrentValue);
    }
}
