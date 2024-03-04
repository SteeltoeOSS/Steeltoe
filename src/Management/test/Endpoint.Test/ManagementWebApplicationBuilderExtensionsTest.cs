// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.Test.Health.TestContributors;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementWebApplicationBuilderExtensionsTest
{
    [Fact]
    public async Task AddDbMigrationsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddDbMigrationsActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IDbMigrationsEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/dbmigrations", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddEnvironmentActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddEnvironmentActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IEnvironmentEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/env", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        await using WebApplication host = hostBuilder.AddHealthActuator().Build();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        Assert.Single(scope.ServiceProvider.GetServices<IHealthEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder_WithTypes()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        await using WebApplication host = hostBuilder.AddHealthActuator(typeof(DownContributor)).Build();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        Assert.Single(scope.ServiceProvider.GetServices<IHealthEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder_WithAggregator()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        await using WebApplication host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), typeof(DownContributor)).Build();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        Assert.Single(scope.ServiceProvider.GetServices<IHealthEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
    }

    [Fact]
    public async Task AddHealthActuator_WebApplicationBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        // start the server, get a client
        await using WebApplication host = hostBuilder.AddHealthActuator().Build();
        host.UseRouting();
        await host.StartAsync();
        HttpClient client = host.GetTestClient();
        await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));

        // request liveness & readiness in order to validate the ApplicationAvailability has been set as expected
        HttpResponseMessage livenessResult = await client.GetAsync(new Uri("actuator/health/liveness", UriKind.Relative));
        HttpResponseMessage readinessResult = await client.GetAsync(new Uri("actuator/health/readiness", UriKind.Relative));
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
        if (Platform.IsWindows)
        {
            WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

            await using WebApplication host = hostBuilder.AddHeapDumpActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<IHeapDumpEndpointHandler>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/heapdump", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task AddHypermediaActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddHypermediaActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IActuatorEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddInfoActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddInfoActuator(new AppSettingsInfoContributor(hostBuilder.Configuration)).Build();

        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IInfoEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggersActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddLoggersActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<ILoggersEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/loggers", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_WebApplicationBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();
        hostBuilder.Logging.AddDynamicSerilog();
        hostBuilder.Logging.AddDynamicConsole();

        await using WebApplication host = hostBuilder.AddLoggersActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/loggers", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMappingsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication host = hostBuilder.AddMappingsActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<RouterMappings>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/mappings", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMetricsActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddMetricsActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IMetricsEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/metrics", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await host.StopAsync();
    }

    [Fact]
    public async Task AddRefreshActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddRefreshActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IRefreshEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().PostAsync(new Uri("/actuator/refresh", UriKind.Relative), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddThreadDumpActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

            await using WebApplication host = hostBuilder.AddThreadDumpActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<IThreadDumpEndpointHandler>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/threaddump", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task AddTraceActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddTraceActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IHttpTraceEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/httptrace", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddServicesActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();

        await using WebApplication host = hostBuilder.AddServicesActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        Assert.Single(host.Services.GetServices<IServicesEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/actuator/beans", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_WebApplicationBuilder()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

        await using WebApplication host = hostBuilder.AddCloudFoundryActuator().Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
    }

    [Fact]
    public async Task AddAllActuators_WebApplicationBuilder_IStartupFilterFires()
    {
        WebApplicationBuilder hostBuilder = GetTestServerWithRouting();
        hostBuilder.Services.AddActionDescriptorCollectionProvider();

        await using WebApplication host = hostBuilder.AddAllActuators().Build();
        host.UseRouting();
        await host.StartAsync();
        HttpClient client = host.GetTestClient();

        Assert.Single(host.Services.GetServices<IActuatorEndpointHandler>());
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_WebApplicationBuilder_IStartupFilterFires()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
        {
            builder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));
            builder.Services.AddActionDescriptorCollectionProvider();
        });

        await host.StartAsync();
        HttpClient client = host.GetTestClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_WebApplicationBuilder_IStartupFilterFires()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "some"); // Allow routing to /cloudfoundryapplication

        WebApplicationBuilder hostBuilder = WebApplication.CreateBuilder();
        hostBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.WebHost.UseTestServer();

        await using WebApplication host = hostBuilder.AddCloudFoundryActuator().Build();
        host.UseRouting();
        await host.StartAsync();

        HttpResponseMessage response = await host.GetTestClient().GetAsync(new Uri("/cloudfoundryapplication", UriKind.Relative));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); // Verify we are hitting the CloudfoundrySecurity Middleware
    }

    [Fact]
    public async Task AddSeveralActuators_WebApplicationBuilder_NoConflict()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
        {
            builder.AddHypermediaActuator().AddInfoActuator().AddHealthActuator().AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).Services
                .AddActionDescriptorCollectionProvider();
        });

        await host.StartAsync();
        HttpClient client = host.GetTestClient();
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_WebApplicationBuilder_PrefersEndpointConfiguration()
    {
        await using WebApplication host = GetTestWebAppWithSecureRouting(builder =>
        {
            builder.AddHypermediaActuator().AddInfoActuator().AddHealthActuator();
            builder.Services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth");
        });

        await host.StartAsync();
        HttpClient client = host.GetTestClient();

        // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
        Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationBuilder GetTestServerWithRouting()
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        return builder;
    }

    private WebApplication GetTestWebAppWithSecureRouting(Action<WebApplicationBuilder>? customizeBuilder = null)
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        customizeBuilder?.Invoke(builder);

        builder.Services.AddRouting();

        builder.Services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        builder.Services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));

        WebApplication app = builder.Build();
        app.UseRouting().UseAuthentication().UseAuthorization();
        return app;
    }
}
