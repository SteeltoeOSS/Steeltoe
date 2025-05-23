// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors;

public sealed class ReadinessStateHealthContributorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "health",
        ["Management:Endpoints:Health:ShowComponents"] = "Always",
        ["Management:Endpoints:Health:ShowDetails"] = "Always"
    };

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHealthActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ReadinessStateContributorOptions options = serviceProvider.GetRequiredService<IOptions<ReadinessStateContributorOptions>>().Value;

        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddHealthActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ReadinessStateContributorOptions options = serviceProvider.GetRequiredService<IOptions<ReadinessStateContributorOptions>>().Value;

        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Reports_success()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, ReadinessStateHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UP",
              "components": {
                "readinessState": {
                  "status": "UP"
                }
              },
              "groups": [
                "readiness"
              ]
            }
            """);
    }

    [Fact]
    public async Task Reports_failure()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, ReadinessStateHealthContributor>();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        var availability = host.Services.GetRequiredService<ApplicationAvailability>();
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, null);

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "OUT_OF_SERVICE",
              "components": {
                "readinessState": {
                  "status": "OUT_OF_SERVICE"
                }
              },
              "groups": [
                "readiness"
              ]
            }
            """);
    }

    [Fact]
    public async Task Reports_unknown()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, ReadinessStateHealthContributor>();

        builder.Services.Remove(builder.Services.First(descriptor =>
            descriptor.ServiceType == typeof(IStartupFilter) && descriptor.ImplementationType == typeof(AvailabilityStartupFilter)));

        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/health"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "status": "UNKNOWN",
              "components": {
                "readinessState": {
                  "status": "UNKNOWN",
                  "description": "Failed to get current availability state"
                }
              },
              "groups": [
                "readiness"
              ]
            }
            """);
    }
}
