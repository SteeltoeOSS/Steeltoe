// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementWebApplicationBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> ExposeAllActuatorsConfiguration = new()
    {
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    [Fact]
    public async Task AddDbMigrationsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddDbMigrationsActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IDbMigrationsEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/dbmigrations", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddEnvironmentActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddEnvironmentActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IEnvironmentEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/env", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        await using WebApplication host = hostBuilder.Build();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<MapActuatorsStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        IHealthContributor[] healthContributors = scope.ServiceProvider.GetServices<IHealthContributor>().ToArray();
        healthContributors.Should().HaveCount(3);
        healthContributors.Should().ContainSingle(contributor => contributor is DiskSpaceContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is LivenessHealthContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is ReadinessHealthContributor);
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder_WithContributor()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        hostBuilder.Services.AddHealthContributor<DownContributor>();
        await using WebApplication host = hostBuilder.Build();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<MapActuatorsStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        scope.ServiceProvider.GetServices<IHealthContributor>().OfType<DownContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddHealthActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        using HttpClient httpClient = host.GetTestClient();
        await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));

        // request liveness & readiness in order to validate the ApplicationAvailability has been set as expected
        HttpResponseMessage livenessResult = await httpClient.GetAsync(new Uri("actuator/health/liveness", UriKind.Relative));
        HttpResponseMessage readinessResult = await httpClient.GetAsync(new Uri("actuator/health/readiness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, livenessResult.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await livenessResult.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, readinessResult.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await readinessResult.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        // confirm that the Readiness state will be changed to refusing traffic when ApplicationStopping fires
        var availability = host.Services.GetRequiredService<ApplicationAvailability>();
        await host.StopAsync();
        Assert.Equal(LivenessState.Correct, availability.GetLivenessState());
        Assert.Equal(ReadinessState.RefusingTraffic, availability.GetReadinessState());
    }

    [Fact]
    public async Task AddHeapDumpActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddHeapDumpActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IHeapDumpEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/heapdump", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHypermediaActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddHypermediaActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IActuatorEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddInfoActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddInfoActuator();

        await using WebApplication host = hostBuilder.Build();

        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IInfoEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggersActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddLoggersActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<ILoggersEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/loggers", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_WebApplicationBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.Logging.AddDynamicSerilog();
        hostBuilder.Logging.AddDynamicConsole();
        hostBuilder.AddLoggersActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/loggers", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMappingsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddMappingsActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<RouterMappings>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/mappings", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMetricsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddMetricsActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IMetricsEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/metrics", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddRefreshActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddRefreshActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IRefreshEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.PostAsync(new Uri("/actuator/refresh", UriKind.Relative), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddThreadDumpActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddThreadDumpActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IThreadDumpEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/threaddump", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHttpExchangesActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddHttpExchangesActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        host.Services.GetServices<IHttpExchangesEndpointHandler>().Should().ContainSingle();
        host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter).Should().ContainSingle();

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/httpexchanges", UriKind.Relative));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddServicesActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddServicesActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IServicesEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator/beans", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_WebApplicationBuilder()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.AddCloudFoundryActuator();

        await using WebApplication host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));
    }

    [Fact]
    public async Task AddAllActuators_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithAllActuatorsExposed();
        hostBuilder.AddAllActuators();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IActuatorEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_WebApplicationBuilder_IStartupFilterFires()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
            builder.AddAllActuators(endpointConventionBuilder => endpointConventionBuilder.RequireAuthorization("TestAuth")));

        await host.StartAsync();

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "some"); // Allow routing to /cloudfoundryapplication

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.AddCloudFoundryActuator();

        await using WebApplication host = hostBuilder.Build();
        host.UseRouting();
        await host.StartAsync();

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/cloudfoundryapplication", UriKind.Relative));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); // Verify we are hitting the CloudfoundrySecurity Middleware
    }

    [Fact]
    public async Task AddSeveralActuators_WebApplicationBuilder_NoConflict()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
        {
            builder.AddHypermediaActuator();
            builder.AddInfoActuator();
            builder.AddHealthActuator();
            builder.AddAllActuators(endpointConventionBuilder => endpointConventionBuilder.RequireAuthorization("TestAuth"));
        });

        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_WebApplicationBuilder_PrefersEndpointConfiguration()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
        {
            builder.AddHypermediaActuator();
            builder.AddInfoActuator();
            builder.AddHealthActuator();
            builder.Services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth");
        });

        await host.StartAsync();

        // these requests hit the "RequireAuthorization" policy and will only pass if GetTestWebAppWithSecureRouting is used
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is MapActuatorsStartupFilter));

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationBuilder GetTestServerWithAllActuatorsExposed()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(ExposeAllActuatorsConfiguration);
        return builder;
    }

    private WebApplication GetTestWebAppWithSecureRouting(Action<WebApplicationBuilder>? customizeBuilder = null)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(ExposeAllActuatorsConfiguration);
        customizeBuilder?.Invoke(builder);

        builder.Services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        builder.Services.AddAuthorizationBuilder().AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read"));

        WebApplication app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
