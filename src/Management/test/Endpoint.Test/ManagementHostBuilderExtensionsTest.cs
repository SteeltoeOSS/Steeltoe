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
using Steeltoe.Management.Endpoint.Test.Health.MockContributors;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementHostBuilderExtensionsTest
{
    private readonly Action<IWebHostBuilder> _testServerWithRouting = builder => builder.UseTestServer()
        .ConfigureServices(s => s.AddRouting().AddActionDescriptorCollectionProvider()).Configure(a => a.UseRouting()).ConfigureAppConfiguration(c =>
            c.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            }));

    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting = builder => builder.UseTestServer().ConfigureServices(s =>
    {
        s.AddRouting();
        s.AddActionDescriptorCollectionProvider();

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
        var epHandler = host.Services.GetService<IDbMigrationsEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddDbMigrationsActuator().StartAsync();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddEnvActuator().Build();
        IEnumerable<IEnvironmentEndpointHandler> epHandler = host.Services.GetServices<IEnvironmentEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddEnvActuator().StartAsync();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHealthActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHealthActuator().Build();
        IEnumerable<IHealthEndpointHandler> epHandler = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
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

        IEnumerable<IHealthEndpointHandler> epHandler = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
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

        IEnumerable<IHealthEndpointHandler> epHandler = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddHealthActuator().StartAsync();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
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
        HttpResponseMessage livenessResult = await client.GetAsync(new Uri("actuator/health/liveness", UriKind.Relative));
        HttpResponseMessage readinessResult = await client.GetAsync(new Uri("actuator/health/readiness", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, livenessResult.StatusCode);
        Assert.Contains("\"LivenessState\":\"CORRECT\"", await livenessResult.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, readinessResult.StatusCode);
        Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await readinessResult.Content.ReadAsStringAsync(), StringComparison.Ordinal);

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
            IEnumerable<IHeapDumpEndpointHandler> epHandler = host.Services.GetServices<IHeapDumpEndpointHandler>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(epHandler);
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

            var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddHypermediaActuator().Build();
        IEnumerable<IActuatorEndpointHandler> epHandler = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddHypermediaActuator().StartAsync();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddInfoActuator().Build();
        var epHandler = host.Services.GetService<IInfoEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(epHandler);
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

        var epHandler = host.Services.GetService<IInfoEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddInfoActuator().StartAsync();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddLoggersActuator().Build();
        IEnumerable<ILoggersEndpointHandler> epHandler = host.Services.GetServices<ILoggersEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IHostBuilder_MultipleLoggersScenarios()
    {
        // Add Serilog + DynamicConsole = runs OK
        IHostBuilder hostBuilder = new HostBuilder().AddDynamicSerilog().AddDynamicLogging().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Add DynamicConsole + Serilog = throws exception
        hostBuilder = new HostBuilder().AddDynamicLogging().AddDynamicSerilog().ConfigureWebHost(_testServerWithRouting);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await hostBuilder.AddLoggersActuator().StartAsync());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMappingsActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddMappingsActuator().Build();

        var mappings = host.Services.GetRequiredService<RouteMappings.RouteMappings>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(mappings);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddMappingsActuator().StartAsync();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddMetricsActuator().Build();
        var epHandler = host.Services.GetService<IMetricsEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddMetricsActuator().StartAsync();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddRefreshActuator().Build();
        IEnumerable<IRefreshEndpointHandler> epHandler = host.Services.GetServices<IRefreshEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddRefreshActuator().StartAsync();

        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().PostAsync(requestUri, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IHostBuilder()
    {
        if (Platform.IsWindows)
        {
            var hostBuilder = new HostBuilder();

            IHost host = hostBuilder.AddThreadDumpActuator().Build();
            IEnumerable<IThreadDumpEndpointHandler> epEndpoint = host.Services.GetServices<IThreadDumpEndpointHandler>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(epEndpoint);
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

            var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddTraceActuator().Build();
        IEnumerable<IHttpTraceEndpointHandler> epHandler = host.Services.GetServices<IHttpTraceEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddTraceActuator().StartAsync();

        var requestUri = new Uri("/actuator/httptrace", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddCloudFoundryActuator().Build();
        var epHandler = host.Services.GetService<ICloudFoundryEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

        using IHost host = await hostBuilder.AddAllActuators().StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting);

        using IHost host = await hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IHostBuilder()
    {
        var hostBuilder = new HostBuilder();

        IHost host = hostBuilder.AddAllActuators().Build();
        IEnumerable<IActuatorEndpointHandler> epHandler = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(epHandler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_NoConflict()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting).AddHypermediaActuator().AddInfoActuator()
            .AddHealthActuator().AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        Assert.Single(host.Services.GetServices<IStartupFilter>());
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_PrefersEndpointConfiguration()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting)
            .ConfigureServices(services => services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth"))

            // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
            .AddHypermediaActuator().AddInfoActuator().AddHealthActuator();

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
        Assert.Single(host.Services.GetServices<IStartupFilter>());
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
