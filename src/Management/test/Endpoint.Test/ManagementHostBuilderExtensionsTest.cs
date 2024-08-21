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
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Steeltoe.Management.Endpoint.Actuators.Trace;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using Steeltoe.Management.Endpoint.Test.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementHostBuilderExtensionsTest
{
    private readonly Action<IWebHostBuilder> _testServerWithAllActuatorsExposed = builder => builder.UseTestServer()
        .Configure(applicationBuilder => applicationBuilder.UseRouting()).ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            }));

    private readonly Action<IWebHostBuilder> _testServerWithSecureRouting = builder => builder.UseTestServer().ConfigureServices(services =>
    {
        services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(applicationBuilder => applicationBuilder.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public void AddDbMigrationsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddDbMigrationsActuator().Build();
        var handler = host.Services.GetService<IDbMigrationsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddDbMigrationsActuator().StartAsync();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvironmentActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddEnvironmentActuator().Build();
        IEnumerable<IEnvironmentEndpointHandler> handlers = host.Services.GetServices<IEnvironmentEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvironmentActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddEnvironmentActuator().StartAsync();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();

        IHealthContributor[] healthContributors = scope.ServiceProvider.GetServices<IHealthContributor>().ToArray();
        healthContributors.Should().HaveCount(3);
        healthContributors.Should().ContainSingle(contributor => contributor is DiskSpaceContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is LivenessHealthContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is ReadinessHealthContributor);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_WithContributor()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        hostBuilder.ConfigureServices(services => services.AddHealthContributor<DownContributor>());
        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();

        scope.ServiceProvider.GetServices<IHealthContributor>().OfType<DownContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddHealthActuator().StartAsync();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

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
        var availability = host.Services.GetRequiredService<ApplicationAvailability>();
        await host.StopAsync();
        Assert.Equal(LivenessState.Correct, availability.GetLivenessState());
        Assert.Equal(ReadinessState.RefusingTraffic, availability.GetReadinessState());
    }

    [Fact]
    public void AddHeapDumpActuator_IHostBuilder()
    {
        if (Platform.IsWindows)
        {
            IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

            using IHost host = hostBuilder.AddHeapDumpActuator().Build();
            IEnumerable<IHeapDumpEndpointHandler> handlers = host.Services.GetServices<IHeapDumpEndpointHandler>();
            IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(handlers);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddHeapDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

            using IHost host = await hostBuilder.AddHeapDumpActuator().StartAsync();

            var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddHypermediaActuator().Build();
        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddHypermediaActuator().StartAsync();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.AddInfoActuator();
        using IHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().FirstOrDefault());
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder_WithExtraContributor()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.AddInfoActuator();
        hostBuilder.ConfigureServices(services => services.AddInfoContributor<TestInfoContributor>());
        using IHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().FirstOrDefault());
        Assert.NotNull(host.Services.GetServices<IInfoContributor>().OfType<TestInfoContributor>().FirstOrDefault());
    }

    [Fact]
    public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddInfoActuator().StartAsync();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddLoggersActuator().Build();
        IEnumerable<ILoggersEndpointHandler> handlers = host.Services.GetServices<ILoggersEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IHostBuilder_MultipleLoggersScenarios()
    {
        // Add Serilog + DynamicConsole = runs OK
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().AddDynamicSerilog().ConfigureLogging(builder => builder.AddDynamicConsole())
            .ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddLoggersActuator().StartAsync();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Add DynamicConsole + Serilog = throws exception
        hostBuilder = TestHostBuilderFactory.Create().ConfigureLogging(builder => builder.AddDynamicConsole()).AddDynamicSerilog()
            .ConfigureWebHost(_testServerWithAllActuatorsExposed);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await hostBuilder.AddLoggersActuator().StartAsync());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMappingsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddMappingsActuator().Build();

        var mappings = host.Services.GetRequiredService<RouterMappings>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(mappings);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddMappingsActuator().StartAsync();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddMetricsActuator().Build();
        var handler = host.Services.GetService<IMetricsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddMetricsActuator().StartAsync();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddRefreshActuator().Build();
        IEnumerable<IRefreshEndpointHandler> handlers = host.Services.GetServices<IRefreshEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

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
            IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

            using IHost host = hostBuilder.AddThreadDumpActuator().Build();
            IEnumerable<IThreadDumpEndpointHandler> handlers = host.Services.GetServices<IThreadDumpEndpointHandler>();
            IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(handlers);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddThreadDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

            using IHost host = await hostBuilder.AddThreadDumpActuator().StartAsync();

            var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddTraceActuator().Build();
        IEnumerable<IHttpTraceEndpointHandler> handlers = host.Services.GetServices<IHttpTraceEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddTraceActuator().StartAsync();

        var requestUri = new Uri("/actuator/httptrace", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddServicesActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddServicesActuator().Build();
        IEnumerable<IServicesEndpointHandler> handlers = host.Services.GetServices<IServicesEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddServicesActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

        using IHost host = await hostBuilder.AddServicesActuator().StartAsync();

        var requestUri = new Uri("/actuator/beans", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddCloudFoundryActuator().Build();
        var handler = host.Services.GetService<ICloudFoundryEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithAllActuatorsExposed);

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
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithSecureRouting);

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
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        using IHost host = hostBuilder.AddAllActuators().Build();
        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_NoConflict()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithSecureRouting).AddHypermediaActuator().AddInfoActuator()
            .AddHealthActuator().AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        IStartupFilter[] startupFilters = host.Services.GetServices<IStartupFilter>().ToArray();
        startupFilters.Should().HaveCount(2);
        startupFilters.Should().ContainSingle(filter => filter is AllActuatorsStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is ManagementPortStartupFilter);

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
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create().ConfigureWebHost(_testServerWithSecureRouting)
            .ConfigureServices(services => services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth"))

            // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
            .AddHypermediaActuator().AddInfoActuator().AddHealthActuator();

        using IHost host = await hostBuilder.StartAsync();
        HttpClient client = host.GetTestServer().CreateClient();

        IStartupFilter[] startupFilters = host.Services.GetServices<IStartupFilter>().ToArray();
        startupFilters.Should().HaveCount(2);
        startupFilters.Should().ContainSingle(filter => filter is AllActuatorsStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is ManagementPortStartupFilter);

        // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
