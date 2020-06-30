// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
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
    public class ManagementHostBuilderExtensionsTest
    {
        [Fact]
        public void AddDbMigrationsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddDbMigrationsActuator().Build();
            var managementEndpoint = host.Services.GetServices<DbMigrationsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<DbMigrationsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddDbMigrationsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddDbMigrationsActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/dbmigrations");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddEnvActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddEnvActuator().Build();
            var managementEndpoint = host.Services.GetServices<EnvEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<EnvStartupFilter>(filter);
        }

        [Fact]
        public async Task AddEnvActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddEnvActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/env");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator().Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator(new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public void AddHealthActuator_IHostBuilder_WithAggregator()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new Type[] { typeof(DownContributor) }).Build();
            var managementEndpoint = host.Services.GetServices<HealthEndpointCore>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HealthStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHealthActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddHealthActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/health");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public async Task AddHealthActuator_IHostBuilder_IStartupFilterFireRegistersAvailabilityEvents()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // start the server, get a client
            var host = await hostBuilder.AddHealthActuator().StartAsync();
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
        public void AddHeapDumpActuator_IHostBuilder()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder();

                // Act
                var host = hostBuilder.AddHeapDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<HeapDumpEndpoint>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
                Assert.Single(managementEndpoint);
                Assert.NotNull(filter);
                Assert.IsType<HeapDumpStartupFilter>(filter);
            }
        }

        [Fact]
        public async Task AddHeapDumpActuator_IHostBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

                // Act
                var host = await hostBuilder.AddHeapDumpActuator().StartAsync();

                // Assert
                var response = host.GetTestServer().CreateClient().GetAsync("/actuator/heapdump");
                Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            }
        }

        [Fact]
        public void AddHypermediaActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddHypermediaActuator().Build();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<HypermediaStartupFilter>(filter);
        }

        [Fact]
        public async Task AddHypermediaActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddHypermediaActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddInfoActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddInfoActuator().Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<InfoStartupFilter>(filter);
        }

        [Fact]
        public void AddInfoActuator_IHostBuilder_WithTypes()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddInfoActuator(new IInfoContributor[] { new AppSettingsInfoContributor(new ConfigurationBuilder().Build()) }).Build();
            var managementEndpoint = host.Services.GetServices<InfoEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<InfoStartupFilter>(filter);
        }

        [Fact]
        public async Task AddInfoActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddInfoActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddLoggersActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddLoggersActuator().Build();
            var managementEndpoint = host.Services.GetServices<LoggersEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<LoggersStartupFilter>(filter);
        }

        [Fact]
        public async Task AddLoggersActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddLoggersActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/loggers");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddMappingsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddMappingsActuator().Build();
            var managementEndpoint = host.Services.GetServices<IRouteMappings>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<MappingsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMappingsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddMappingsActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/mappings");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddMetricsActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddMetricsActuator().Build();
            var managementEndpoint = host.Services.GetServices<MetricsEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<MetricsStartupFilter>(filter);
        }

        [Fact]
        public async Task AddMetricsActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddMetricsActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/metrics");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddRefreshActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddRefreshActuator().Build();
            var managementEndpoint = host.Services.GetServices<RefreshEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<RefreshStartupFilter>(filter);
        }

        [Fact]
        public async Task AddRefreshActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddRefreshActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/refresh");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddThreadDumpActuator_IHostBuilder()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder();

                // Act
                var host = hostBuilder.AddThreadDumpActuator().Build();
                var managementEndpoint = host.Services.GetServices<ThreadDumpEndpoint_v2>();
                var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

                // Assert
                Assert.Single(managementEndpoint);
                Assert.NotNull(filter);
                Assert.IsType<ThreadDumpStartupFilter>(filter);
            }
        }

        [Fact]
        public async Task AddThreadDumpActuator_IHostBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                // Arrange
                var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

                // Act
                var host = await hostBuilder.AddThreadDumpActuator().StartAsync();

                // Assert
                var response = host.GetTestServer().CreateClient().GetAsync("/actuator/threaddump");
                Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            }
        }

        [Fact]
        public void AddTraceActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddTraceActuator().Build();
            var managementEndpoint = host.Services.GetServices<HttpTraceEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<TraceStartupFilter>(filter);
        }

        [Fact]
        public async Task AddTraceActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddTraceActuator().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator/httptrace");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddCloudFoundryActuator_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddCloudFoundryActuator().Build();
            var managementEndpoint = host.Services.GetServices<CloudFoundryEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<CloudFoundryActuatorStartupFilter>(filter);
        }

        [Fact]
        public async Task AddAllActuators_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddAllActuators().StartAsync();

            // Assert
            var response = host.GetTestServer().CreateClient().GetAsync("/actuator");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/actuator/info");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
            response = host.GetTestServer().CreateClient().GetAsync("/actuator/health");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        [Fact]
        public void AddaLLActuators_IHostBuilder()
        {
            // Arrange
            var hostBuilder = new HostBuilder();

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
        public async Task AddCloudFoundryActuator_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(testServerWithRouting);

            // Act
            var host = await hostBuilder.AddCloudFoundryActuator().StartAsync();

            var response = host.GetTestServer().CreateClient().GetAsync("/cloudfoundryapplication");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.Result.StatusCode);
        }

        private Action<IWebHostBuilder> testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
    }
}
