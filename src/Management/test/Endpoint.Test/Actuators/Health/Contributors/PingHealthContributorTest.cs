// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Contributors;

public sealed class PingHealthContributorTest
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

        PingContributorOptions options = serviceProvider.GetRequiredService<IOptions<PingContributorOptions>>().Value;

        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:Ping:Enabled"] = "false"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddHealthActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        PingContributorOptions options = serviceProvider.GetRequiredService<IOptions<PingContributorOptions>>().Value;

        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Reports_success()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddHealthActuator();
        builder.Services.RemoveAll<IHealthContributor>();
        builder.Services.AddSingleton<IHealthContributor, PingHealthContributor>();
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
                "ping": {
                  "status": "UP"
                }
              }
            }
            """);
    }
}
