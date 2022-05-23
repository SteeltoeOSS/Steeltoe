// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using Steeltoe.Connector;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.Kubernetes;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Extensions.Configuration.RandomValue;
using Steeltoe.Extensions.Logging;
using Steeltoe.Extensions.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Bootstrap.Autoconfig.Test
{
    public class WebApplicationBuilderExtensionsTest
    {
        [Fact]
        public void ConfigServerConfiguration_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_ConfigServerCore, SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

            // WebApplication.CreateBuilder() automatically includes a few builders
            Assert.Equal(9, config.Providers.Count());
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void CloudFoundryConfiguration_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

            Assert.Equal(8, config.Providers.Count());
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        }

        [Fact(Skip = "Requires Kubernetes")]
        public void KubernetesConfiguration_IsAutowired()
        {
            Environment.SetEnvironmentVariable("KUBERNETES_SERVICE_HOST", "TEST");
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_KubernetesCore);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

            Assert.Equal(11, config.Providers.Count());
            Assert.Equal(2, config.Providers.OfType<KubernetesConfigMapProvider>().Count());
            Assert.Equal(2, config.Providers.OfType<KubernetesSecretProvider>().Count());
        }

        [Fact]
        public void RandomValueConfiguration_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_RandomValueBase);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

            Assert.Equal(8, config.Providers.Count());
            Assert.Single(config.Providers.OfType<RandomValueProvider>());
        }

        [Fact]
        public void PlaceholderResolver_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Configuration_PlaceholderBase);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;
            Assert.Single(config.Providers.OfType<PlaceholderResolverProvider>());
        }

        [Fact]
        public void Connectors_AreAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Connector_ConnectorCore);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;
            var services = host.Services;

            Assert.Equal(8, config.Providers.Count());
            Assert.Single(config.Providers.OfType<ConnectionStringConfigurationProvider>());
            Assert.NotNull(services.GetService<MySql.Data.MySqlClient.MySqlConnection>());
            Assert.NotNull(services.GetService<MongoDB.Driver.MongoClient>());
            Assert.NotNull(services.GetService<Oracle.ManagedDataAccess.Client.OracleConnection>());
            Assert.NotNull(services.GetService<Npgsql.NpgsqlConnection>());
            Assert.NotNull(services.GetService<RabbitMQ.Client.ConnectionFactory>());
            Assert.NotNull(services.GetService<StackExchange.Redis.ConnectionMultiplexer>());
            Assert.NotNull(services.GetService<System.Data.SqlClient.SqlConnection>());
        }

        [Fact]
        public void DynamicSerilog_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Extensions_Logging_DynamicSerilogCore);

            var loggerProvider = (IDynamicLoggerProvider)host.Services.GetService(typeof(IDynamicLoggerProvider));

            Assert.IsType<SerilogDynamicProvider>(loggerProvider);
        }

        [Fact]
        public void ServiceDiscoveryBase_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Discovery_ClientBase);
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();

            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        }

        [Fact]
        public void ServiceDiscoveryCore_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Discovery_ClientCore);
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();

            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        }

        [Fact]
        public async Task KubernetesActuators_AreAutowired()
        {
            var webApp = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Management_KubernetesCore);
            webApp.UseRouting();
            await webApp.StartAsync();

            var managementEndpoint = webApp.Services.GetServices<ActuatorEndpoint>();
            var filter = webApp.Services.GetServices<IStartupFilter>().FirstOrDefault();
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);

            await ActuatorTestAsync(webApp.GetTestClient());
        }

        [Fact]
        public async Task AllActuators_AreAutowired()
        {
            var webApp = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Management_EndpointCore);
            webApp.UseRouting();
            await webApp.StartAsync();

            var managementEndpoint = webApp.Services.GetServices<ActuatorEndpoint>();
            var filter = webApp.Services.GetServices<IStartupFilter>().FirstOrDefault(f => f is AllActuatorsStartupFilter);

            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);

            await ActuatorTestAsync(webApp.GetTestClient());
        }

        [Fact]
        public async Task WavefrontMetricsExporter_IsAutowired()
        {
            var webAppBuilder = WebApplication.CreateBuilder();
            webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers._wavefrontConfiguration);
            var exclusions = new List<string> { SteeltoeAssemblies.Steeltoe_Management_EndpointCore };
            webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(exclusions));
            webAppBuilder.WebHost.UseTestServer();
            var webApp = webAppBuilder.Build();

            webApp.UseRouting();
            await webApp.StartAsync();
            var exporter = webApp.Services.GetService<WavefrontMetricsExporter>();

            Assert.NotNull(exporter);
        }

        [Fact]
        public async Task WavefrontTraceExporter_IsAutowired()
        {
            var webAppBuilder = WebApplication.CreateBuilder();
            webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers._wavefrontConfiguration);
            var exclusions = new List<string> { SteeltoeAssemblies.Steeltoe_Management_TracingCore };
            webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(exclusions));
            webAppBuilder.WebHost.UseTestServer();
            var webApp = webAppBuilder.Build();

            webApp.UseRouting();
            await webApp.StartAsync();

            var tracerProvider = webApp.Services.GetService<TracerProvider>();
            Assert.NotNull(tracerProvider);
            var processor = tracerProvider.GetType().GetProperty("Processor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tracerProvider);
            var exporter = processor.GetType().GetField("exporter", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(processor);
            Assert.NotNull(exporter);
            Assert.IsType<WavefrontTraceExporter>(exporter);
        }

        [Fact]
        public void TracingBase_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Management_TracingBase);
            var tracerProvider = host.Services.GetService<TracerProvider>();

            Assert.NotNull(host.Services.GetService<IHostedService>());
            Assert.NotNull(host.Services.GetService<ITracingOptions>());
            Assert.NotNull(tracerProvider);
            Assert.NotNull(host.Services.GetService<IDynamicMessageProcessor>());

            // confirm instrumentation(s) were added as expected
            var instrumentations = tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tracerProvider) as List<object>;
            Assert.NotNull(instrumentations);
            Assert.Single(instrumentations);
            Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));
            Assert.DoesNotContain(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore"));
        }

        [Fact]
        public void TracingCore_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Management_TracingCore);
            var tracerProvider = host.Services.GetService<TracerProvider>();

            Assert.NotNull(host.Services.GetService<IHostedService>());
            Assert.NotNull(host.Services.GetService<ITracingOptions>());
            Assert.NotNull(tracerProvider);
            Assert.NotNull(host.Services.GetService<IDynamicMessageProcessor>());

            // confirm instrumentation(s) were added as expected
            var instrumentations = tracerProvider.GetType().GetField("instrumentations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tracerProvider) as List<object>;
            Assert.NotNull(instrumentations);
            Assert.Equal(2, instrumentations.Count);
            Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));
            Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore"));
        }

        [Fact]
        public void CloudFoundryContainerSecurity_IsAutowired()
        {
            var host = GetWebApplicationWithSteeltoe(SteeltoeAssemblies.Steeltoe_Security_Authentication_CloudFoundryCore);
            var config = host.Services.GetServices<IConfiguration>().First(c => c is ConfigurationManager) as IConfigurationRoot;

            Assert.Equal(8, config.Providers.Count());
            Assert.Single(config.Providers.OfType<PemCertificateProvider>());
            Assert.NotNull(host.Services.GetRequiredService<IOptions<CertificateOptions>>());
            Assert.NotNull(host.Services.GetRequiredService<ICertificateRotationService>());
            Assert.NotNull(host.Services.GetRequiredService<IAuthorizationHandler>());
        }

        private WebApplication GetWebApplicationWithSteeltoe(params string[] steeltoeInclusions)
        {
            var webAppBuilder = WebApplication.CreateBuilder();
            webAppBuilder.Configuration.AddInMemoryCollection(TestHelpers._fastTestsConfiguration);
            webAppBuilder.AddSteeltoe(SteeltoeAssemblies.AllAssemblies.Except(steeltoeInclusions));
            webAppBuilder.WebHost.UseTestServer();
            return webAppBuilder.Build();
        }

        private async Task ActuatorTestAsync(HttpClient testClient)
        {
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
        }
    }
}

#endif