// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementWebHostBuilderExtensionsTest
{
    [Fact]
    public void AddDbMigrationsActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddDbMigrationsActuator().Build();
        var managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddDbMigrationsActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/dbmigrations");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddEnvActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddEnvActuator().Build();
        var managementEndpoint = host.Services.GetServices<EnvEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddEnvActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddEnvActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/env");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddHealthActuator().Build();
        var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder_WithTypes()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddHealthActuator(new Type[] { typeof(DownContributor) }).Build();
        var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddHealthActuator_IWebHostBuilder_WithAggregator()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new Type[] { typeof(DownContributor) }).Build();
        var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddHealthActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
    {
        var hostBuilder = _testServerWithRouting;

        // start the server, get a client
        var host = hostBuilder.AddHealthActuator().Start();
        var client = host.GetTestClient();

        // request liveness & readiness in order to validate the ApplicationAvailability has been set as expected
        var livenessResult = await client.GetAsync("actuator/health/liveness");
        var readinessResult = await client.GetAsync("actuator/health/readiness");
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
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            var host = hostBuilder.AddHeapDumpActuator().Build();
            var managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

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
            var hostBuilder = _testServerWithRouting;

            var host = hostBuilder.AddHeapDumpActuator().Start();

            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/heapdump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddHypermediaActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddHypermediaActuator().Build();
        var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddHypermediaActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddInfoActuator().Build();
        var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public void AddInfoActuator_IWebHostBuilder_WithTypes()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddInfoActuator(new IInfoContributor[] { new AppSettingsInfoContributor(new ConfigurationBuilder().Build()) }).Build();
        var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddInfoActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggersActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddLoggersActuator().Build();
        var managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddLoggersActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
    {
        // Add Serilog + DynamicConsole = runs OK
        var hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());

        var host = hostBuilder.AddLoggersActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
    {
        // Add DynamicConsole + Serilog = throws exception
        var hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());

        var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.AddLoggersActuator().Start());

        Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message);
    }

    [Fact]
    public void AddMappingsActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddMappingsActuator().Build();
        var managementEndpoint = host.Services.GetServices<IRouteMappings>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMappingsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddMappingsActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/mappings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddMetricsActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddMetricsActuator().Build();
        var managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddMetricsActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddRefreshActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddRefreshActuator().Build();
        var managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddRefreshActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/refresh");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddThreadDumpActuator_IWebHostBuilder()
    {
        if (Platform.IsWindows)
        {
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            var host = hostBuilder.AddThreadDumpActuator().Build();
            var managementEndpoint = host.Services.GetServices<ThreadDumpEndpoint_v2>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

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
            var hostBuilder = _testServerWithRouting;

            var host = hostBuilder.AddThreadDumpActuator().Start();

            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/threaddump");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public void AddTraceActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddTraceActuator().Build();
        var managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddTraceActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddTraceActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/httptrace");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddCloudFoundryActuator_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddCloudFoundryActuator().Build();
        var managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddAllActuators().Start();
        var client = host.GetTestServer().CreateClient();

        var response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddAllActuatorsWithConventions_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithSecureRouting;

        var host = hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
        var client = host.GetTestServer().CreateClient();

        var response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddAllActuators_IWebHostBuilder()
    {
        var hostBuilder = new WebHostBuilder().Configure((b) => { });

        var host = hostBuilder.AddAllActuators().Build();
        var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
        var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

        Assert.Single(managementEndpoint);
        Assert.NotNull(filter);
        Assert.IsType<AllActuatorsStartupFilter>(filter);
    }

    [Fact]
    public async Task AddCloudFoundryActuator_IWebHostBuilder_IStartupFilterFires()
    {
        var hostBuilder = _testServerWithRouting;

        var host = hostBuilder.AddCloudFoundryActuator().Start();

        var response = await host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
    {
        var hostBuilder = _testServerWithSecureRouting
            .AddHypermediaActuator()
            .AddInfoActuator()
            .AddHealthActuator()
            .AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

        var host = hostBuilder.Start();
        var client = host.GetTestServer().CreateClient();

        Assert.Single(host.Services.GetServices<IStartupFilter>());
        var response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddSeveralActuators_IWebHostBuilder_PrefersEndpointConfiguration()
    {
        var hostBuilder =
            _testServerWithSecureRouting
                .ConfigureServices(services => services.ActivateActuatorEndpoints(ep => ep.RequireAuthorization("TestAuth")))

                // each of these will try to add their own AllActuatorsStartupFilter but should no-op in favor of the above
                .AddHypermediaActuator()
                .AddInfoActuator()
                .AddHealthActuator();

        var host = hostBuilder.Start();
        var client = host.GetTestServer().CreateClient();

        // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
        Assert.Single(host.Services.GetServices<IStartupFilter>());
        var response = await client.GetAsync("/actuator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response = await client.GetAsync("/actuator/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void AddWavefront_IWebHostBuilder()
    {
        var wfSettings = new Dictionary<string, string>()
        {
            { "management:metrics:export:wavefront:uri", "https://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", "testToken" }
        };

        var hostBuilder = new WebHostBuilder().Configure(configure => { }).ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(wfSettings));
        var host = hostBuilder.AddWavefrontMetrics().Build();

        var diagnosticsManagers = host.Services.GetServices<IDiagnosticsManager>();
        Assert.Single(diagnosticsManagers);
        var diagnosticServices = host.Services.GetServices<IHostedService>().OfType<DiagnosticServices>();
        Assert.Single(diagnosticServices);
        var options = host.Services.GetServices<IMetricsObserverOptions>();
        Assert.Single(options);
        var viewRegistry = host.Services.GetServices<IViewRegistry>();
        Assert.Single(viewRegistry);
        var exporters = host.Services.GetServices<WavefrontMetricsExporter>();
        Assert.Single(exporters);
    }

    [Fact]
    public void AddWavefront_ProxyConfigIsValid()
    {
        var wfSettings = new Dictionary<string, string>()
        {
            { "management:metrics:export:wavefront:uri", "proxy://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", string.Empty } // Should not throw
        };

        var hostBuilder = new WebHostBuilder().Configure(configure => { }).ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(wfSettings));
        var host = hostBuilder.AddWavefrontMetrics().Build();

        var exporters = host.Services.GetServices<WavefrontMetricsExporter>();
        Assert.Single(exporters);
    }

    [Fact]
    public async Task AddAllActuators_Doesnt_Interfere_With_OpenTelemetryExtensions_Called_Before_SteeltoeExtensions()
    {
        var hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>() { ["management:endpoints:actuator:exposure:include:0"] = "*" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using (var unConsole = new ConsoleOutputBorrower())
        {
            var host = hostBuilder
                .ConfigureServices(services => services.AddOpenTelemetryMetrics(
                    builder => builder
                        .AddMeter("TestMeter")
                        .AddConsoleExporter((opts, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000)))
                .AddAllActuators()
                .Start();
            var client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            var counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(3000); // wait for metrics to be collected
            var response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is printed to Console
            var output = unConsole.ToString();
            Assert.Contains("Warning", output);
            Assert.Contains("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);
        }
    }

    [Fact]
    public async Task AddAllActuators_Doesnt_Interfere_With_OpenTelemetryExtensions_Called_With_SteeltoeExtensions()
    {
        var hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>() { ["management:endpoints:actuator:exposure:include:0"] = "*" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using (var unConsole = new ConsoleOutputBorrower())
        {
            var host = hostBuilder
                .ConfigureServices(services => services.AddOpenTelemetryMetrics(
                    builder => builder
                        .ConfigureSteeltoeMetrics()
                        .AddMeter("TestMeter")
                        .AddConsoleExporter((opts, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000)))
                .AddAllActuators()
                .Start();
            var client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            var counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(3000); // wait for metrics to be collected
            var response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is printed to Console
            var output = unConsole.ToString();
            Assert.Contains("Warning", output);
            Assert.Contains("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);

            // Assert Steeltoe configuration is respected
            Assert.Contains("Export clr.process.uptime", output);
        }
    }

    [Fact]
    public async Task AddAllActuators_Doesnt_Interfere_With_OpenTelemetryExtensions_Called_After_SteeltoeExtensions()
    {
        var hostBuilder = _testServerWithRouting;

        var appSettings = new Dictionary<string, string>() { ["management:endpoints:actuator:exposure:include:0"] = "*" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        using (var unConsole = new ConsoleOutputBorrower())
        {
            var host = hostBuilder
                .AddAllActuators()
                .ConfigureServices(services => services.AddOpenTelemetryMetrics(
                    builder => builder
                        .AddMeter("TestMeter")
                        .AddConsoleExporter((opts, mrOpts) => mrOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000)))
                .Start();
            var client = host.GetTestServer().CreateClient();

            var meter = new Meter("TestMeter");
            var counter = meter.CreateCounter<int>("TestCounter");
            counter.Add(1);

            await Task.Delay(5000); // wait for metrics to be collected
            var response = await client.GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert warning is not printed to Console
            var output = unConsole.ToString();
            Assert.DoesNotContain("Warning", output);
            Assert.DoesNotContain("OpenTelemetry for Steeltoe", output);

            // Assert Otel configuration is respected
            Assert.Contains("Export TestCounter, Meter: TestMeter", output);
        }
    }

    private readonly IWebHostBuilder _testServerWithRouting = new WebHostBuilder().UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
    private readonly IWebHostBuilder _testServerWithSecureRouting =
        new WebHostBuilder().UseTestServer()
            .ConfigureServices(s =>
            {
                s.AddRouting();
                s.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
            })
            .Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());
}