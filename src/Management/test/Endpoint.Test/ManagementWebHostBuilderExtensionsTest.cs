// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Diagnostics;
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
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementWebHostBuilderExtensionsTest
{
    private readonly IWebHostBuilder _testServerWithRouting =
        new WebHostBuilder().UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());

    private readonly IWebHostBuilder _testServerWithSecureRouting = new WebHostBuilder().UseTestServer().ConfigureServices(s =>
    {
        s.AddRouting();

        s.AddAuthentication(TestAuthHandler.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme,
            _ =>
            {
            });

        s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
    }).Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());

    [Fact]
    public void AddDbMigrationsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddDbMigrationsActuator().Build();
        IEnumerable<DbMigrationsEndpoint> managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddDbMigrationsActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/dbmigrations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddEnvActuator().Build();
        IEnumerable<EnvEndpoint> managementEndpoint = host.Services.GetServices<EnvEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddEnvActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/env");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddHealthActuator().Build();
        IEnumerable<HealthEndpointCore> managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder_WithTypes()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddHealthActuator(new[]
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
    public void AddHealthActuator_IWebHostBuilder_WithAggregator()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new[]
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
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddHealthActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
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
    public void AddHeapDumpActuator_IWebHostBuilder()
    {
        if (Platform.IsWindows)
        {
            IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
            {
            });

            IWebHost host = hostBuilder.AddHeapDumpActuator().Build();
            IEnumerable<HeapDumpEndpoint> managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(managementEndpoint);
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

            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/heapdump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddHypermediaActuator().Build();
        IEnumerable<ActuatorEndpoint> managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddHypermediaActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddInfoActuator().Build();
        IEnumerable<InfoEndpoint> managementEndpoint = host.Services.GetServices<InfoEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder_WithTypes()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddInfoActuator(new IInfoContributor[]
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
    public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddInfoActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddLoggersActuator().Build();
        IEnumerable<LoggersEndpoint> managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddLoggersActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        IWebHostBuilder hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());

        using IWebHost host = hostBuilder.AddLoggersActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
    {
        // Add DynamicConsole + Serilog = throws exception
        IWebHostBuilder hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());

        var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.AddLoggersActuator().Start());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message);
    }

    [Fact]
    public void AddMappingsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddMappingsActuator().Build();
        IEnumerable<IRouteMappings> managementEndpoint = host.Services.GetServices<IRouteMappings>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddMappingsActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/mappings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddMetricsActuator().Build();
        IEnumerable<MetricsEndpoint> managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddMetricsActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddRefreshActuator().Build();
        IEnumerable<RefreshEndpoint> managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddRefreshActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/refresh");
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

            IWebHost host = hostBuilder.AddThreadDumpActuator().Build();
            IEnumerable<ThreadDumpEndpointV2> managementEndpoint = host.Services.GetServices<ThreadDumpEndpointV2>();
            IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            Assert.Single(managementEndpoint);
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

            HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/threaddump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddTraceActuator().Build();
        IEnumerable<HttpTraceEndpoint> managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddTraceActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/actuator/httptrace");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddCloudFoundryActuator().Build();
        IEnumerable<CloudFoundryEndpoint> managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddAllActuators().Start();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting;

        using IWebHost host = hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
        HttpClient client = host.GetTestServer().CreateClient();

        HttpResponseMessage response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IWebHostBuilder()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.AddAllActuators().Build();
        IEnumerable<ActuatorEndpoint> managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        IStartupFilter filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_IWebHostBuilder_IStartupFilterFires()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        using IWebHost host = hostBuilder.AddCloudFoundryActuator().Start();

        HttpResponseMessage response = await host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting.AddHypermediaActuator().AddInfoActuator().AddHealthActuator()
            .AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        using IWebHost host = hostBuilder.Start();
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
    public async Task AddSeveralActuators_IWebHostBuilder_PrefersEndpointConfiguration()
    {
        IWebHostBuilder hostBuilder = _testServerWithSecureRouting
            .ConfigureServices(services => services.ActivateActuatorEndpoints(ep => ep.RequireAuthorization("TestAuth")))

            // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
            .AddHypermediaActuator().AddInfoActuator().AddHealthActuator();

        using IWebHost host = hostBuilder.Start();
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

    [Fact]
    public void AddWavefront_IWebHostBuilder()
    {
        var wfSettings = new Dictionary<string, string>
        {
            { "management:metrics:export:wavefront:uri", "https://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", "testToken" }
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));

        IWebHost host = hostBuilder.AddWavefrontMetrics().Build();

        IEnumerable<IDiagnosticsManager> diagnosticsManagers = host.Services.GetServices<IDiagnosticsManager>();
        Assert.Single(diagnosticsManagers);
        IEnumerable<DiagnosticServices> diagnosticServices = host.Services.GetServices<IHostedService>().OfType<DiagnosticServices>();
        Assert.Single(diagnosticServices);
        IEnumerable<IMetricsObserverOptions> options = host.Services.GetServices<IMetricsObserverOptions>();
        Assert.Single(options);
        IEnumerable<IViewRegistry> viewRegistry = host.Services.GetServices<IViewRegistry>();
        Assert.Single(viewRegistry);
        IEnumerable<WavefrontMetricsExporter> exporters = host.Services.GetServices<WavefrontMetricsExporter>();
        Assert.Single(exporters);
    }

    [Fact]
    public void AddWavefront_ProxyConfigIsValid()
    {
        var wfSettings = new Dictionary<string, string>
        {
            { "management:metrics:export:wavefront:uri", "proxy://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", string.Empty } // Should not throw
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));

        IWebHost host = hostBuilder.AddWavefrontMetrics().Build();

        IEnumerable<WavefrontMetricsExporter> exporters = host.Services.GetServices<WavefrontMetricsExporter>();
        Assert.Single(exporters);
    }

    [Fact]
    public async Task AddAllActuators_DoesNot_Interfere_With_OpenTelemetryExtensions_Called_Before_SteeltoeExtensions()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        using (var unConsole = new ConsoleOutputBorrower())
        {
            using IWebHost host = hostBuilder.ConfigureServices(services => services.AddOpenTelemetryMetrics(builder =>
                    builder.AddMeter("TestMeter")
                        .AddConsoleExporter((_, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000)))
                .AddAllActuators().Start();

            HttpClient client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            Counter<int> counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(3000); // wait for metrics to be collected
            HttpResponseMessage response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is printed to Console
            string output = unConsole.ToString();
            Assert.Contains("Warning", output);
            Assert.Contains("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);
        }
    }

    [Fact]
    public async Task AddAllActuators_DoesNot_Interfere_With_OpenTelemetryExtensions_Called_With_SteeltoeExtensions()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        using (var unConsole = new ConsoleOutputBorrower())
        {
            using IWebHost host = hostBuilder.ConfigureServices(services => services.AddOpenTelemetryMetrics(builder =>
                    builder.ConfigureSteeltoeMetrics().AddMeter("TestMeter")
                        .AddConsoleExporter((_, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000))).AddAllActuators()
                .Start();

            HttpClient client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            Counter<int> counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(3000); // wait for metrics to be collected
            HttpResponseMessage response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is printed to Console
            string output = unConsole.ToString();
            Assert.Contains("Warning", output);
            Assert.Contains("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);

            // Assert Steeltoe configuration is respected
            Assert.Contains("Export clr.process.uptime", output);
        }
    }

    [Fact]
    public async Task AddAllActuators_DoesNot_Interfere_With_OpenTelemetryExtensions_Called_After_SteeltoeExtensions()
    {
        IWebHostBuilder hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        using (var unConsole = new ConsoleOutputBorrower())
        {
            using IWebHost host = hostBuilder.AddAllActuators().ConfigureServices(services => services.AddOpenTelemetryMetrics(builder =>
                    builder.AddMeter("TestMeter")
                        .AddConsoleExporter((_, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000)))
                .Start();

            HttpClient client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            Counter<int> counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(5000); // wait for metrics to be collected
            HttpResponseMessage response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is not printed to Console
            string output = unConsole.ToString();
            Assert.DoesNotContain("Warning", output);
            Assert.DoesNotContain("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);
        }
    }
}
