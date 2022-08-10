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
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Test;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementHostBuilderExtensionsTest
{
    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder =>
        builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting = builder => builder.UseTestServer().ConfigureServices(s =>
    {
        s.AddRouting();

        s.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme,
            _ =>
            {
            });

        s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public void AddDbMigrationsActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddDbMigrationsActuator().Build();
        IEnumerable<DbMigrationsEndpoint> managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddDbMigrationsActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/dbmigrations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddEnvActuator().Build();
        IEnumerable<EnvEndpoint> managementEndpoint = host.Services.GetServices<EnvEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddEnvActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/env");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHealthActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHealthActuator().Build();
        IEnumerable<HealthEndpointCore> managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IHostBuilder_WithTypes()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHealthActuator(new[]
        {
            typeof(DownContributor)
        }).Build();

        IEnumerable<HealthEndpointCore> managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IHostBuilder_WithAggregator()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new[]
        {
            typeof(DownContributor)
        }).Build();

        IEnumerable<HealthEndpointCore> managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddHealthActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        // start the server, get a client
        using IHost host = await hostBuilder.AddHealthActuator().StartAsync();
        HttpClient client = host.GetTestClient();

        // request liveness & readiness in order to validate the ApplicationAvailability has been set as expected
        HttpResponseMessage livenessResult = await client.GetAsync("actuator/health/liveness");
        HttpResponseMessage readinessResult = await client.GetAsync("actuator/health/readiness");
        Assert.Equal(HttpStatusCode.OK, livenessResult.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await livenessResult.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, readinessResult.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await readinessResult.Content.ReadAsStringAsync());

        // confirm that the Readiness state will be changed to refusing traffic when ApplicationStopping fires
        var availability = host.Services.GetService<ApplicationAvailability>();
        await host.StopAsync();
        Assert.Equal(LivenessState.Correct, availability.GetLivenessState());
        Assert.Equal(ReadinessState.RefusingTraffic, availability.GetReadinessState());
    }

    [Fact]
    public void AddHeapDumpActuator_IHostBuilder()
    {
        if (Platform.IsWindows)
        {
            var hostBuilder = new HostBuilder();

            IHost host = hostBuilder.AddHeapDumpActuator().Build();
            IEnumerable<HeapDumpEndpoint> managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddHeapDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            using IHost host = await hostBuilder.AddHeapDumpActuator().StartAsync();

            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/heapdump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHypermediaActuator().Build();
        IEnumerable<ActuatorEndpoint> managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddHypermediaActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddInfoActuator().Build();
        IEnumerable<InfoEndpoint> managementEndpoint = host.Services.GetServices<InfoEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder_WithTypes()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddInfoActuator(new IInfoContributor[]
        {
            new AppSettingsInfoContributor(new ConfigurationBuilder().Build())
        }).Build();

        IEnumerable<InfoEndpoint> managementEndpoint = host.Services.GetServices<InfoEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddInfoActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddLoggersActuator().Build();
        IEnumerable<LoggersEndpoint> managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IHostBuilder_MultipleLoggersScenarios()
    {
        // Add Serilog + DynamicConsole = runs OK
        IHostBuilder hostBuilder = new HostBuilder().AddDynamicSerilog().AddDynamicLogging().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Add DynamicConsole + Serilog = throws exception
        hostBuilder = new HostBuilder().AddDynamicLogging().AddDynamicSerilog().ConfigureWebHost(_testServerWithRouting);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await hostBuilder.AddLoggersActuator().StartAsync());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message);
    }

    [Fact]
    public void AddMappingsActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddMappingsActuator().Build();
        IEnumerable<IRouteMappings> managementEndpoint = host.Services.GetServices<IRouteMappings>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddMappingsActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/mappings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddMetricsActuator().Build();
        IEnumerable<MetricsEndpoint> managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddMetricsActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddRefreshActuator().Build();
        IEnumerable<RefreshEndpoint> managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddRefreshActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/refresh");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IHostBuilder()
    {
        if (Platform.IsWindows)
        {
            var hostBuilder = new HostBuilder();

            IHost host = hostBuilder.AddThreadDumpActuator().Build();
            IEnumerable<ThreadDumpEndpointV2> managementEndpoint = host.Services.GetServices<ThreadDumpEndpointV2>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddThreadDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            using IHost host = await hostBuilder.AddThreadDumpActuator().StartAsync();

            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/threaddump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddTraceActuator().Build();
        IEnumerable<HttpTraceEndpoint> managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddTraceActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/httptrace");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddCloudFoundryActuator().Build();
        IEnumerable<CloudFoundryEndpoint> managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddAllActuators().StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting);

        using IHost host = await hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddAllActuators().Build();
        IEnumerable<ActuatorEndpoint> managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddCloudFoundryActuator().StartAsync();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_NoConflict()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting).AddHypermediaActuator().AddInfoActuator()
            .AddHealthActuator().AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        Assert.Single(host.Services.GetServices<IStartupFilter>());
        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_PrefersEndpointConfiguration()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting)
            .ConfigureServices(services => services.ActivateActuatorEndpoints(ep => ep.RequireAuthorization("TestAuth")))

            // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
            .AddHypermediaActuator().AddInfoActuator().AddHealthActuator();

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
        Assert.Single(host.Services.GetServices<IStartupFilter>());
        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
