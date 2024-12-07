// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;

namespace Steeltoe.Management.Prometheus.Test;

public sealed class PrometheusExtensionsTest
{
    [Fact]
    public async Task AddPrometheusActuator_SetsUpRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddPrometheusActuator();

        await using ServiceProvider provider = services.BuildServiceProvider(true);

        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PrometheusEndpointOptions>>();
        optionsMonitor.CurrentValue.Path.Should().Be("prometheus");

        IEnumerable<IStartupFilter>? startupFilters = provider.GetServices<IStartupFilter>();
        startupFilters.Should().ContainSingle(filter => filter is ConfigurePrometheusActuatorStartupFilter);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddPrometheusActuator_MapsExporter(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddPrometheusActuator();
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Contain("# EOF");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddPrometheusActuator_MapsExporterWithBranchedPipeline(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme).AddBearerToken();
                services.AddAuthorizationBuilder().AddPolicy("test", policy => policy.RequireClaim("scope", "test"));
                services.ConfigureActuatorEndpoints(endpoints => endpoints.RequireAuthorization("test"));

                services.AddPrometheusActuator(true, branchedBuilder =>
                {
                    branchedBuilder.UseAuthorization();
                });
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddPrometheusActuator_CloudFoundry404WhenDependenciesMissing(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "cf_api": "https://api.cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"
            }
            """);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddPrometheusActuator();
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response = await httpClient.GetAsync(new Uri("http://localhost/cloudfoundryapplication/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task AddPrometheusActuator_MapsExporterForCloudFoundry(HostBuilderType hostBuilderType)
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "cf_api": "https://api.cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"
            }
            """);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configuration => configuration.AddCloudFoundry());

            builder.ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddCloudFoundryActuator();
                services.AddPrometheusActuator();
            });
        });

        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response = await httpClient.GetAsync(new Uri("http://localhost/cloudfoundryapplication/prometheus"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
