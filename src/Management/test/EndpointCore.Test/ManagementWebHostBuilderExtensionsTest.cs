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
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementWebHostBuilderExtensionsTest
    {
        [Fact]
        public void AddDbMigrationsActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddDbMigrationsActuator().Build();
            var managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddDbMigrationsActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddDbMigrationsActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/dbmigrations");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddEnvActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddEnvActuator().Build();
            var managementEndpoint = host.Services.GetServices<EnvEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddEnvActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddEnvActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/env");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddHealthActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddHealthActuator().Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IWebHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddHealthActuator(new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IWebHostBuilder_WithAggregator()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddHealthActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddHealthActuator_IWebHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
        {
            // Arrange
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
                // Arrange
                var hostBuilder = new WebHostBuilder().Configure((b) => { });

                // Act
                var host = hostBuilder.AddHeapDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
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
                // Arrange
                var hostBuilder = _testServerWithRouting;

                // Act
                var host = hostBuilder.AddHeapDumpActuator().Start();

                // Assert
                var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/heapdump");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public void AddHypermediaActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddHypermediaActuator().Build();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHypermediaActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddHypermediaActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddInfoActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddInfoActuator().Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public void AddInfoActuator_IWebHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddInfoActuator(new IInfoContributor[] { new AppSettingsInfoContributor(new ConfigurationBuilder().Build()) }).Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddInfoActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddInfoActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddLoggersActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddLoggersActuator().Build();
            var managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddLoggersActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddLoggersActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddLoggers_IWebHostBuilder_MultipleLoggersScenario1()
        {
            // Add Serilog + DynamicConsole = runs OK
            // Arrange
            var hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicSerilog().AddDynamicConsole());

            // Act
            var host = hostBuilder.AddLoggersActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddLoggers_IWebHostBuilder_MultipleLoggersScenario2()
        {
            // Add DynamicConsole + Serilog = throws exception
            // Arrange
            var hostBuilder = _testServerWithRouting.ConfigureLogging(builder => builder.AddDynamicConsole().AddDynamicSerilog());

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => hostBuilder.AddLoggersActuator().Start());

            // Assert
            Assert.Contains("An IDynamicLoggerProvider has already been configured! Call 'AddDynamicSerilog' earlier", exception.Message);
        }

        [Fact]
        public void AddMappingsActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddMappingsActuator().Build();
            var managementEndpoint = host.Services.GetServices<IRouteMappings>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMappingsActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddMappingsActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/mappings");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddMetricsActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddMetricsActuator().Build();
            var managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMetricsActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddMetricsActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddRefreshActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddRefreshActuator().Build();
            var managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddRefreshActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddRefreshActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/refresh");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddThreadDumpActuator_IWebHostBuilder()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new WebHostBuilder().Configure((b) => { });

                // Act
                var host = hostBuilder.AddThreadDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<ThreadDumpEndpoint_v2>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
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
                // Arrange
                var hostBuilder = _testServerWithRouting;

                // Act
                var host = hostBuilder.AddThreadDumpActuator().Start();

                // Assert
                var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/threaddump");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public void AddTraceActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddTraceActuator().Build();
            var managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddTraceActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddTraceActuator().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddCloudFoundryActuator_IWebHostBuilder()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddCloudFoundryActuator().Build();
            var managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddAllActuators_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddAllActuators().Start();

            // Assert
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddAllActuatorsWithConventions_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithSecureRouting;

            // Act
            var host = hostBuilder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
            var client = host.GetTestServer().CreateClient();

            // Assert
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
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure((b) => { });

            // Act
            var host = hostBuilder.AddAllActuators().Build();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddCloudFoundryActuator_IWebHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = _testServerWithRouting;

            // Act
            var host = hostBuilder.AddCloudFoundryActuator().Start();

            var response = await host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddSeveralActuators_IWebHostBuilder_NoConflict()
        {
            // Arrange
            var hostBuilder = _testServerWithSecureRouting
                                .AddHypermediaActuator()
                                .AddInfoActuator()
                                .AddHealthActuator()
                                .AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));

            // Act
            var host = hostBuilder.Start();

            // Assert
            Assert.Single(host.Services.GetServices<IStartupFilter>());
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
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

            // Act
            var host = hostBuilder.Start();

            // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
            // Assert
            Assert.Single(host.Services.GetServices<IStartupFilter>());
            var response = await host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await host.GetTestServer().CreateClient().GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
}
