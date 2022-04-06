// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
using Steeltoe.Management.OpenTelemetry.Exporters;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementWebApplicationBuilderExtensionsTest
    {
        [Fact]
        public async Task AddDbMigrationsActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddDbMigrationsActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<DbMigrationsEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/dbmigrations");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddEnvActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddEnvActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<EnvEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/env");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddHealthActuator_WebApplicationBuilder()
        {
            var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

            var host = hostBuilder.AddHealthActuator().Build();

            Assert.Single(host.Services.GetServices<HealthEndpointCore>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        }

        [Fact]
        public void AddHealthActuator_WebApplicationBuilder_WithTypes()
        {
            var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

            var host = hostBuilder.AddHealthActuator(new Type[] { typeof(DownContributor) }).Build();

            Assert.Single(host.Services.GetServices<HealthEndpointCore>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        }

        [Fact]
        public void AddHealthActuator_WebApplicationBuilder_WithAggregator()
        {
            var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

            var host = hostBuilder.AddHealthActuator(new DefaultHealthAggregator(), new Type[] { typeof(DownContributor) }).Build();

            Assert.Single(host.Services.GetServices<HealthEndpointCore>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        }

        [Fact]
        public async Task AddHealthActuator_WebApplicationBuilder_IStartupFilterFireRegistersAvailabilityEvents()
        {
            var hostBuilder = GetTestServerWithRouting();

            // start the server, get a client
            var host = hostBuilder.AddHealthActuator().Build();
            host.UseRouting();
            await host.StartAsync();
            var client = host.GetTestClient();
            var response = await client.GetAsync("/actuator/health");

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
        public async Task AddHeapDumpActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                var hostBuilder = GetTestServerWithRouting();

                var host = hostBuilder.AddHeapDumpActuator().Build();
                host.UseRouting();
                await host.StartAsync();

                Assert.Single(host.Services.GetServices<HeapDumpEndpoint>());
                Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
                var response = await host.GetTestClient().GetAsync("/actuator/heapdump");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task AddHypermediaActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddHypermediaActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<ActuatorEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddInfoActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddInfoActuator(new IInfoContributor[] { new AppSettingsInfoContributor(hostBuilder.Configuration) }).Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<InfoEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddLoggersActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddLoggersActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<LoggersEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/loggers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddLoggers_WebApplicationBuilder_MultipleLoggersScenario1()
        {
            // Add Serilog + DynamicConsole = runs OK
            var hostBuilder = GetTestServerWithRouting();
            hostBuilder.Logging.AddDynamicSerilog();
            hostBuilder.Logging.AddDynamicConsole();

            var host = hostBuilder.AddLoggersActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            var response = await host.GetTestClient().GetAsync("/actuator/loggers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddMappingsActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddMappingsActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<IRouteMappings>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/mappings");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddMetricsActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddMetricsActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<MetricsEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/metrics");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await host.StopAsync();
        }

        [Fact]
        public async Task AddRefreshActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddRefreshActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<RefreshEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/refresh");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddThreadDumpActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            if (Platform.IsWindows)
            {
                var hostBuilder = GetTestServerWithRouting();

                var host = hostBuilder.AddThreadDumpActuator().Build();
                host.UseRouting();
                await host.StartAsync();

                Assert.Single(host.Services.GetServices<ThreadDumpEndpoint_v2>());
                Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
                var response = await host.GetTestClient().GetAsync("/actuator/threaddump");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task AddTraceActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddTraceActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            Assert.Single(host.Services.GetServices<HttpTraceEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await host.GetTestClient().GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddCloudFoundryActuator_WebApplicationBuilder()
        {
            var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();

            var host = hostBuilder.AddCloudFoundryActuator().Build();

            Assert.Single(host.Services.GetServices<CloudFoundryEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
        }

        [Fact]
        public async Task AddAllActuators_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddAllActuators().Build();
            host.UseRouting();
            await host.StartAsync();
            var client = host.GetTestClient();

            Assert.Single(host.Services.GetServices<ActuatorEndpoint>());
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await client.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddAllActuatorsWithConventions_WebApplicationBuilder_IStartupFilterFires()
        {
            var host = GetTestWebAppWithSecureRouting(builder => builder.AddAllActuators(ep => ep.RequireAuthorization("TestAuth")));

            await host.StartAsync();
            var client = host.GetTestClient();

            var response = await client.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddCloudFoundryActuator_WebApplicationBuilder_IStartupFilterFires()
        {
            var hostBuilder = GetTestServerWithRouting();

            var host = hostBuilder.AddCloudFoundryActuator().Build();
            host.UseRouting();
            await host.StartAsync();

            var response = await host.GetTestClient().GetAsync("/cloudfoundryapplication");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddSeveralActuators_WebApplicationBuilder_NoConflict()
        {
            var host = GetTestWebAppWithSecureRouting(s =>
                                {
                                    s.AddHypermediaActuator()
                                        .AddInfoActuator()
                                        .AddHealthActuator()
                                        .AddAllActuators(ep => ep.RequireAuthorization("TestAuth"));
                                });

            await host.StartAsync();
            var client = host.GetTestClient();
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await client.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddSeveralActuators_WebApplicationBuilder_PrefersEndpointConfiguration()
        {
            var host =
                GetTestWebAppWithSecureRouting(builder =>
                {
                    builder
                        .AddHypermediaActuator()
                        .AddInfoActuator()
                        .AddHealthActuator();
                    builder.Services.ActivateActuatorEndpoints(ep => ep.RequireAuthorization("TestAuth"));
                });

            await host.StartAsync();
            var client = host.GetTestClient();

            // these requests hit the "RequireAuthorization" policy and will only pass if _testServerWithSecureRouting is used
            Assert.Single(host.Services.GetServices<IStartupFilter>().Where(filter => filter is AllActuatorsStartupFilter));
            var response = await client.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await client.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void AddWavefrontExporter()
        {
            var host =
                  GetTestWebAppWithSecureRouting(builder =>
                  {
                      builder
                          .AddWavefrontMetrics();
                  });
            var exporter = host.Services.GetService<WavefrontMetricsExporter>();
            Assert.NotNull(exporter);
        }

        private WebApplicationBuilder GetTestServerWithRouting()
        {
            var builder = TestHelpers.GetTestWebApplicationBuilder();
            return builder;
        }

        private WebApplication GetTestWebAppWithSecureRouting(Action<WebApplicationBuilder> customizeBuilder = null)
        {
            var builder = TestHelpers.GetTestWebApplicationBuilder();
            customizeBuilder?.Invoke(builder);

            builder.Services.AddRouting();
            builder.Services
                .AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            builder.Services.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));

            var app = builder.Build();
            app.UseRouting().UseAuthentication().UseAuthorization();
            return app;
        }
    }
}
#endif