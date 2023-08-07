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
using Steeltoe.Management.Endpoint.Test.Health.TestContributors;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ManagementWebHostBuilderExtensionsTest : BaseTest
{
    private readonly IWebHostBuilder _testServerWithRouting = new WebHostBuilder().UseTestServer()
        .ConfigureServices(services => services.AddRouting().AddActionDescriptorCollectionProvider())
        .Configure(applicationBuilder => applicationBuilder.UseRouting()).ConfigureAppConfiguration(configurationBuilder =>
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["management:endpoints:actuator:exposure:include:0"] = "*"
            }));

    private readonly IWebHostBuilder _testServerWithSecureRouting = new WebHostBuilder().UseTestServer().ConfigureServices(services =>
    {
        services.AddRouting();
        services.AddActionDescriptorCollectionProvider();

        services.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
            TestAuthHandler.AuthenticationScheme, _ =>
            {
            });

        services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(applicationBuilder => applicationBuilder.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public void AddDbMigrationsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddDbMigrationsActuator().Build();
        var handler = host.Services.GetService<IDbMigrationsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddDbMigrationsActuator().Start();

        var requestUri = new Uri("/actuator/dbmigrations", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvironmentActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddEnvironmentActuator().Build();
        IEnumerable<IEnvironmentEndpointHandler> handlers = host.Services.GetServices<IEnvironmentEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvironmentActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddEnvironmentActuator().Start();

        var requestUri = new Uri("/actuator/env", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddHealthActuator().Build();
        IEnumerable<IHealthEndpointHandler> handlers = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder_WithTypes()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddHealthActuator(typeof(DownContributor)).Build();

        IEnumerable<IHealthEndpointHandler> handlers = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder_WithAggregator()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), typeof(DownContributor)).Build();

        IEnumerable<IHealthEndpointHandler> handlers = host.Services.GetServices<IHealthEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddHealthActuator().Start();

        var requestUri = new Uri("/actuator/health", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        // start the server, get a client
        using IWebHost host = hostBuilder.AddHealthActuator().Start();
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
            IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
            {
            });

            using IWebHost host = hostBuilder.AddHeapDumpActuator().Build();
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
            IWebHostBuilder hostBuilder = _testServerWithRouting;

            using IWebHost host = hostBuilder.AddHeapDumpActuator().Start();

            var requestUri = new Uri("/actuator/heapdump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddHypermediaActuator().Build();
        IEnumerable<IActuatorEndpointHandler> handlers = host.Services.GetServices<IActuatorEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddHypermediaActuator().Start();

        var requestUri = new Uri("/actuator", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddInfoActuator().Build();
        var handler = host.Services.GetService<IInfoEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder_WithTypes()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddInfoActuator(new AppSettingsInfoContributor(new ConfigurationBuilder().Build())).Build();

        var handler = host.Services.GetService<IInfoEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handler);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddInfoActuator().Start();

        var requestUri = new Uri("/actuator/info", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddLoggersActuator().Build();
        IEnumerable<ILoggersEndpointHandler> handlers = host.Services.GetServices<ILoggersEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddLoggersActuator().Start();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        IWebHostBuilder hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());

        using IWebHost host = hostBuilder.AddLoggersActuator().Start();

        var requestUri = new Uri("/actuator/loggers", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
    {
        // Add DynamicConsole + Serilog = throws exception
        IWebHostBuilder hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());

        var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.AddLoggersActuator().Start());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMappingsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddMappingsActuator().Build();
        IEnumerable<RouterMappings> mappings = host.Services.GetServices<RouterMappings>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(mappings);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddMappingsActuator().Start();

        var requestUri = new Uri("/actuator/mappings", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddMetricsActuator().Build();
        IEnumerable<IMetricsEndpointHandler> handlers = host.Services.GetServices<IMetricsEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddMetricsActuator().Start();

        var requestUri = new Uri("/actuator/metrics", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddRefreshActuator().Build();
        IEnumerable<IRefreshEndpointHandler> handlers = host.Services.GetServices<IRefreshEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddRefreshActuator().Start();

        var requestUri = new Uri("/actuator/refresh", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().PostAsync(requestUri, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IWebHostBuilder()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
            {
            });

            using IWebHost host = hostBuilder.AddThreadDumpActuator().Build();
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
            IWebHostBuilder hostBuilder = _testServerWithRouting;

            using IWebHost host = hostBuilder.AddThreadDumpActuator().Start();

            var requestUri = new Uri("/actuator/threaddump", UriKind.Relative);
            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddTraceActuator().Build();
        IEnumerable<IHttpTraceEndpointHandler> handlers = host.Services.GetServices<IHttpTraceEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddTraceActuator().Start();

        var requestUri = new Uri("/actuator/httptrace", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddCloudFoundryActuator().Build();
        IEnumerable<ICloudFoundryEndpointHandler> handlers = host.Services.GetServices<ICloudFoundryEndpointHandler>();
        IStartupFilter? filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.NotNull(handlers);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddAllActuators().Start();
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

        using IWebHost host = hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
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
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        using IWebHost host = hostBuilder.AddAllActuators().Build();
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

        using IWebHost host = _testServerWithRouting.AddCloudFoundryActuator().Start();

        var requestUri = new Uri("/cloudfoundryapplication", UriKind.Relative);
        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync(requestUri);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting.AddHypermediaActuator().AddInfoActuator().AddHealthActuator()
            .AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        using IWebHost host = hostBuilder.Start();
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
    public async Task AddSeveralActuators_IWebHostBuilder_PrefersEndpointConfiguration()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting
            .ConfigureServices(services => services.ActivateActuatorEndpoints().RequireAuthorization("TestAuth"))

            // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
            .AddHypermediaActuator().AddInfoActuator().AddHealthActuator();

        using IWebHost host = hostBuilder.Start();
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
