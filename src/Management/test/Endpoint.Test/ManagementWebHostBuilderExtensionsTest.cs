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

public sealed class ManagementWebHostBuilderExtensionsTest : BaseTest
{
    private readonly IWebHostBuilder _testServerWithAllActuatorsExposed = TestWebHostBuilderFactory.Create().UseTestServer()
        .Configure(applicationBuilder => applicationBuilder.UseRouting()).ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            }));

    private readonly IWebHostBuilder _testServerWithSecureRouting = TestWebHostBuilderFactory.Create().UseTestServer().ConfigureServices(services =>
    {
        services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(applicationBuilder => applicationBuilder.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public void AddDbMigrationsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddDbMigrationsActuator();
        using IWebHost host = hostBuilder.Build();

        var handler = host.Services.GetService<IDbMigrationsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddDbMigrationsActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvironmentActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddEnvironmentActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IEnvironmentEndpointHandler> handlers = host.Services.GetServices<IEnvironmentEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvironmentActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddEnvironmentActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        using IWebHost host = hostBuilder.Build();

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
    public async Task AddHealthActuator_IWebHostBuilder_WithContributor()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddHealthActuator();
        hostBuilder.ConfigureServices(services => services.AddHealthContributor<DownContributor>());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetServices<IHealthContributor>().OfType<DownContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddHealthActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddHealthActuator();
        using IWebHost host = hostBuilder.Start();

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
    public void AddHeapDumpActuator_IWebHostBuilder()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
            hostBuilder.AddHeapDumpActuator();
            using IWebHost host = hostBuilder.Build();

            IEnumerable<IHeapDumpEndpointHandler> handlers = host.Services.GetServices<IHeapDumpEndpointHandler>();
            IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(handlers);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddHeapDumpActuator_IWebHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
            hostBuilder.AddHeapDumpActuator();
            using IWebHost host = hostBuilder.Start();

            var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddHypermediaActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddHypermediaActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddInfoActuator();
        using IWebHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().FirstOrDefault());
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder_WithExtraContributor()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddInfoActuator();
        hostBuilder.ConfigureServices(services => services.AddInfoContributor<TestInfoContributor>());
        using IWebHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<AllActuatorsStartupFilter>().FirstOrDefault());
        Assert.NotNull(host.Services.GetServices<IInfoContributor>().OfType<TestInfoContributor>().FirstOrDefault());
    }

    [Fact]
    public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddInfoActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddLoggersActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<ILoggersEndpointHandler> handlers = host.Services.GetServices<ILoggersEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddLoggersActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());
        hostBuilder.AddLoggersActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
    {
        // Add DynamicConsole + Serilog = throws exception
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());
        hostBuilder.AddLoggersActuator();

        var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.Start());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMappingsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddMappingsActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<RouterMappings> mappings = host.Services.GetServices<RouterMappings>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(mappings);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddMappingsActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddMetricsActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IMetricsEndpointHandler> handlers = host.Services.GetServices<IMetricsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddMetricsActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddRefreshActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IRefreshEndpointHandler> handlers = host.Services.GetServices<IRefreshEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddRefreshActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().PostAsync(requestUri, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IWebHostBuilder()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
            hostBuilder.AddThreadDumpActuator();
            using IWebHost host = hostBuilder.Build();

            IEnumerable<IThreadDumpEndpointHandler> handlers = host.Services.GetServices<IThreadDumpEndpointHandler>();
            IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(handlers);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }
    }

    [Fact]
    public async Task AddThreadDumpActuator_IWebHostBuilder_IStartupFilterFires()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
            hostBuilder.AddThreadDumpActuator();
            using IWebHost host = hostBuilder.Start();

            var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddTraceActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IHttpTraceEndpointHandler> handlers = host.Services.GetServices<IHttpTraceEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddTraceActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/httptrace", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddServicesActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddServicesActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IServicesEndpointHandler> handlers = host.Services.GetServices<IServicesEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddServicesActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddServicesActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/actuator/beans", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddCloudFoundryActuator();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<ICloudFoundryEndpointHandler> handlers = host.Services.GetServices<ICloudFoundryEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddAllActuators();
        using IWebHost host = hostBuilder.Start();

        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting;
        hostBuilder.AddAllActuators(builder => builder.RequireAuthorization("TestAuth"));
        using IWebHost host = hostBuilder.Start();

        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.AddAllActuators();
        using IWebHost host = hostBuilder.Build();

        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_IWebHostBuilder_IStartupFilterFires()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "some"); // Allow routing to /cloudfoundryapplication

        IWebHostBuilder hostBuilder = _testServerWithAllActuatorsExposed;
        hostBuilder.AddCloudFoundryActuator();
        using IWebHost host = hostBuilder.Start();

        var requestUri = new Uri("/cloudfoundryapplication", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting;
        hostBuilder.AddHypermediaActuator();
        hostBuilder.AddInfoActuator();
        hostBuilder.AddHealthActuator();
        hostBuilder.AddAllActuators(builder => builder.RequireAuthorization("TestAuth"));
        using IWebHost host = hostBuilder.Start();

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
    public async Task AddSeveralActuators_IWebHostBuilder_PrefersEndpointConfiguration()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting;

        hostBuilder.ConfigureServices(services => services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth"));

        // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
        hostBuilder.AddHypermediaActuator();
        hostBuilder.AddInfoActuator();
        hostBuilder.AddHealthActuator();

        using IWebHost host = hostBuilder.Start();
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
