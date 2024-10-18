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

public sealed class ManagementHostBuilderExtensionsTest
{
    private static readonly Action<IWebHostBuilder> ConfigureWebHostWithAllActuatorsExposed = builder =>
    {
        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            var appSettingsWithAllActuatorsExposed = new Dictionary<string, string?>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            };

            configurationBuilder.AddInMemoryCollection(appSettingsWithAllActuatorsExposed);
        });
    };

    private static readonly Action<IWebHostBuilder> ConfigureWebHostWithSecureRouting = builder =>
    {
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
    };

    [Fact]
    public void AddDbMigrationsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddDbMigrationsActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IDbMigrationsEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddDbMigrationsActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvironmentActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddEnvironmentActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IEnvironmentEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddEnvironmentActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddEnvironmentActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();
        host.Services.GetService<IHealthEndpointHandler>().Should().NotBeNull();

        IHealthContributor[] healthContributors = host.Services.GetServices<IHealthContributor>().ToArray();
        healthContributors.Should().HaveCount(3);
        healthContributors.Should().ContainSingle(contributor => contributor is DiskSpaceContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is LivenessHealthContributor);
        healthContributors.Should().ContainSingle(contributor => contributor is ReadinessHealthContributor);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_WithContributor()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddHealthActuator();
            services.AddHealthContributor<DownContributor>();
        });

        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().HaveCount(1);
        host.Services.GetService<IHealthAggregator>().Should().NotBeNull();
        host.Services.GetService<IHealthEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IHealthContributor>().OfType<DownContributor>().Should().HaveCount(1);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddHealthActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient client = host.GetTestClient();

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
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHeapDumpActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IHeapDumpEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHeapDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddHeapDumpActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHypermediaActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHypermediaActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IActuatorEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddHypermediaActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddInfoActuator());
        using IHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().FirstOrDefault());
    }

    [Fact]
    public void AddInfoActuator_IHostBuilder_WithExtraContributor()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddInfoActuator();
            services.AddInfoContributor<TestInfoContributor>();
        });

        using IHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<IInfoEndpointHandler>());
        Assert.NotNull(host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().FirstOrDefault());
        Assert.NotNull(host.Services.GetServices<IInfoContributor>().OfType<TestInfoContributor>().FirstOrDefault());
    }

    [Fact]
    public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddInfoActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<ILoggersEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IHostBuilder_MultipleLoggersScenarios()
    {
        // Add Serilog + DynamicConsole = runs OK
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.AddDynamicSerilog();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole());
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Add DynamicConsole + Serilog = throws exception
        hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureLogging(builder => builder.AddDynamicConsole());
        hostBuilder.AddDynamicSerilog();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddLoggersActuator());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await hostBuilder.StartAsync());
        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRouteMappingsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddRouteMappingsActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<RouterMappings>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddRouteMappingsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddRouteMappingsActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddMetricsActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IMetricsEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddMetricsActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddRefreshActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IRefreshEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddRefreshActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);
        HttpResponseMessage response = await httpClient.PostAsync(requestUri, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddThreadDumpActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IThreadDumpEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddThreadDumpActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddThreadDumpActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHttpExchangesActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddHttpExchangesActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IHttpExchangesEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddHttpExchangesActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddHttpExchangesActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/httpexchanges", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void AddServicesActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddServicesActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IServicesEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddServicesActuator_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddServicesActuator());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("/actuator/beans", UriKind.Relative);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddCloudFoundryActuator());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<ICloudFoundryEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddAllActuators_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithAllActuatorsExposed);
        hostBuilder.ConfigureServices(services => services.AddAllActuators());

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IHostBuilder_IStartupFilterFires()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithSecureRouting);

        hostBuilder.ConfigureServices(services =>
        {
            services.AddAllActuators();
            services.ConfigureActuatorEndpoints(endpoints => endpoints.RequireAuthorization("TestAuth"));
        });

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IHostBuilder()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddAllActuators());
        using IHost host = hostBuilder.Build();

        host.Services.GetService<IActuatorEndpointHandler>().Should().NotBeNull();
        host.Services.GetServices<IStartupFilter>().OfType<ConfigureActuatorsMiddlewareStartupFilter>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddSeveralActuators_IHostBuilder_NoConflict()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.CreateWeb();
        hostBuilder.ConfigureWebHost(ConfigureWebHostWithSecureRouting);

        hostBuilder.ConfigureServices(services =>
        {
            services.AddHypermediaActuator();
            services.AddInfoActuator();
            services.AddHealthActuator();
            services.AddAllActuators();
            services.ConfigureActuatorEndpoints(endpoints => endpoints.RequireAuthorization("TestAuth"));
        });

        using IHost host = await hostBuilder.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        IStartupFilter[] startupFilters = host.Services.GetServices<IStartupFilter>().ToArray();
        startupFilters.Should().ContainSingle(filter => filter is ConfigureActuatorsMiddlewareStartupFilter);
        startupFilters.Should().ContainSingle(filter => filter is ManagementPortStartupFilter);

        // these requests hit the "RequireAuthorization" policy and will only pass if ConfigureWebHostWithSecureRouting is used
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("/actuator", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/info", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await httpClient.GetAsync(new Uri("/actuator/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
