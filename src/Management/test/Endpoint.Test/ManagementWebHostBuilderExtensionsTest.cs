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
using Steeltoe.Management.Endpoint.Actuators.All;
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
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using Steeltoe.Management.Endpoint.Test.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementWebHostBuilderExtensionsTest : BaseTest
{
    [Fact]
    public void AddDbMigrationsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddDbMigrationsActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IDbMigrationsEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddDbMigrationsActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvironmentActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddEnvironmentActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IEnvironmentEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddEnvironmentActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddEnvironmentActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().HaveCount(1);
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

        hostBuilder.ConfigureServices(services =>
        {
            services.AddHealthActuator();
            services.AddHealthContributor<DownContributor>();
        });

        using IWebHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

        scope.ServiceProvider.GetService<IHealthEndpointHandler>().Should().NotBeNull();
        scope.ServiceProvider.GetServices<IHealthContributor>().OfType<DownContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

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
    public void AddHeapDumpActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHeapDumpActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IHeapDumpEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHeapDumpActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddHeapDumpActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHypermediaActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHypermediaActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IActuatorEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddHypermediaActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddInfoActuator());
        using IWebHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().FirstOrDefault());
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder_WithExtraContributor()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddInfoActuator();
            services.AddInfoContributor<TestInfoContributor>();
        });

        using IWebHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().FirstOrDefault());
        Assert.NotNull(host.Services.GetServices<IInfoContributor>().OfType<TestInfoContributor>().FirstOrDefault());
    }

    [Fact]
    public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddInfoActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<ILoggersEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
    {
        // Add DynamicConsole + Serilog = throws exception
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        var exception = Assert.Throws<InvalidOperationException>(hostBuilder.Build);

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRouteMappingsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddRouteMappingsActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<RouterMappings>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddRouteMappingsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddRouteMappingsActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddMetricsActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IMetricsEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddMetricsActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddRefreshActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IRefreshEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddRefreshActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);
        HttpResponseMessage response = await httpClient.PostAsync(requestUri, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddThreadDumpActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IThreadDumpEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddThreadDumpActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddThreadDumpActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHttpExchangesActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHttpExchangesActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IHttpExchangesEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHttpExchangesActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddHttpExchangesActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/httpexchanges", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void AddServicesActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddServicesActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IServicesEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddServicesActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddServicesActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/beans", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddCloudFoundryActuator());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<ICloudFoundryEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddAllActuators());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithSecureRouting();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddAllActuators();
            services.ConfigureActuatorEndpoints(endpoints => endpoints.RequireAuthorization("TestAuth"));
        });

        using IWebHost host = hostBuilder.Build();
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
    public void AddAllActuators_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddAllActuators());
        using IWebHost host = hostBuilder.Build();

        host.Services.GetService<IActuatorEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddCloudFoundryActuator_IWebHostBuilder_IStartupFilterFires()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", "some"); // Allow routing to /cloudfoundryapplication

        IWebHostBuilder hostBuilder = GetWebHostBuilderWithAllActuatorsExposed();
        hostBuilder.ConfigureServices(services => services.AddCloudFoundryActuator());

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/cloudfoundryapplication", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
    {
        IWebHostBuilder hostBuilder = GetWebHostBuilderWithSecureRouting();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddHypermediaActuator();
            services.AddInfoActuator();
            services.AddHealthActuator();
            services.AddAllActuators();
            services.ConfigureActuatorEndpoints(endpoints => endpoints.RequireAuthorization("TestAuth"));
        });

        using IWebHost host = hostBuilder.Build();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        IStartupFilter[] startupFilters = host.Services.GetServices<IStartupFilter>().ToArray();
        startupFilters.Should().HaveCount(2);
        startupFilters.Should().ContainSingle(filter => filter is ConfigureActuatorsMiddlewareStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is ManagementPortStartupFilter);

        // these requests hit the "RequireAuthorization" policy and will only pass if WebHostBuilderWithSecureRouting is used
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static IWebHostBuilder GetWebHostBuilderWithAllActuatorsExposed()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            var appSettingsWithAllActuatorsExposed = new Dictionary<string, string?>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            };

            configurationBuilder.AddInMemoryCollection(appSettingsWithAllActuatorsExposed);
        });

        return builder;
    }

    private static IWebHostBuilder GetWebHostBuilderWithSecureRouting()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create();

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme, _ =>
                {
                });

            services.AddAuthorizationBuilder().AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read"));
        });

        builder.Configure(applicationBuilder =>
        {
            applicationBuilder.UseAuthentication();
            applicationBuilder.UseAuthorization();
        });

        return builder;
    }
}
