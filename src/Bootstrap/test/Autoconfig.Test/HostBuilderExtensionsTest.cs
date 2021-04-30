// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.Placeholder;
using Steeltoe.Extensions.Configuration.RandomValue;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Bootstrap.Autoconfig.Test
{
    public class HostBuilderExtensionsTest
    {
        private static readonly Dictionary<string, string> _fastTests = new ()
        {
            { "spring:cloud:config:timeout", "10" },
            { "eureka:client:shouldRegister", "true" },
            { "eureka:client:eurekaServer:connectTimeoutSeconds", "1" },
            { "eureka:client:eurekaServer:retryCount", "0" },
        };

        [Fact]
        public void ConfigServerConfiguration_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string>
                {
                    SteeltoeAssemblies.Steeltoe_Extensions_Configuration_ConfigServerCore,
                    SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore
                });
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(_fastTests));

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // assert
            Assert.Equal(4, config.Providers.Count());
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void CloudFoundryConfiguration_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Extensions_Configuration_CloudFoundryCore });
            var hostBuilder = new HostBuilder();

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // assert
            Assert.Equal(2, config.Providers.Count());
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        }

        // TODO: test kubernetes config autowiring
        ////[Fact]
        ////public void KubernetesConfiguration_IsAutowired()
        ////{
        ////}

        [Fact]
        public void RandomValueConfiguration_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Extensions_Configuration_RandomValueBase })
                .ToList();
            var hostBuilder = new HostBuilder();

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // assert
            Assert.Equal(2, config.Providers.Count());
            Assert.Single(config.Providers.OfType<RandomValueProvider>());
        }

        [Fact]
        public void PlaceholderResolver_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Extensions_Configuration_PlaceholderCore });
            var hostBuilder = new HostBuilder();

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // assert
            Assert.Single(config.Providers);
            Assert.Single(config.Providers.OfType<PlaceholderResolverProvider>());
        }

        // TODO: test connectors autowiring
        // TODO: test EF autowiring
        ////[Fact]
        ////public void Connectors_AreAutowired()
        ////{
        ////}

        [Fact]
        public void DynamicSerilog_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Extensions_Logging_DynamicSerilogCore });
            var hostBuilder = new HostBuilder();

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();

            // assert
            var logger = (Logger)host.Services.GetService(typeof(Logger));
            var loggerSinksField = logger.GetType().GetField("_sink", BindingFlags.NonPublic | BindingFlags.Instance);
            var aggregatedSinks = loggerSinksField.GetValue(logger);
            var aggregateSinksField = aggregatedSinks.GetType().GetField("_sinks", BindingFlags.NonPublic | BindingFlags.Instance);
            var sinks = (ILogEventSink[])aggregateSinksField.GetValue(aggregatedSinks);
            Assert.Single(sinks);
            Assert.Equal("Serilog.Sinks.SystemConsole.ConsoleSink", sinks.First().GetType().FullName);
        }

        [Fact]
        public void ServiceDiscoveryBase_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Discovery_ClientBase });
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(_fastTests));

            // act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();

            // assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        }

        [Fact]
        public void ServiceDiscoveryCore_IsAutowired()
        {
            // arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Discovery_ClientCore });

            // act
            var host = new HostBuilder()
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(_fastTests))
                .AddSteeltoe(exclusions).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();

            // assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        }

        [Fact]
        public async Task KubernetesActuators_AreAutowired()
        {
            // Arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Management_KubernetesCore });
            var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            // Act
            var host = await hostBuilder.AddSteeltoe(exclusions).StartAsync();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
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

        [Fact]
        public async Task CloudFoundryActuators_AreAutowired()
        {
            // Arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Management_CloudFoundryCore });
            var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            // Act
            var host = await hostBuilder.AddSteeltoe(exclusions).StartAsync();
            var managementOptions = host.Services.GetServices<IManagementOptions>();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            Assert.Contains(managementOptions, t => t.GetType() == typeof(CloudFoundryManagementOptions));
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

        [Fact]
        public void AllActuators_AreAutowired()
        {
            // Arrange
            var exclusions = SteeltoeAssemblies.AllAssemblies
                .Except(new List<string> { SteeltoeAssemblies.Steeltoe_Management_EndpointCore });
            var hostBuilder = new HostBuilder();

            // Act
            var host = hostBuilder.AddSteeltoe(exclusions).Build();
            var managementEndpoint = host.Services.GetServices<ActuatorEndpoint>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(managementEndpoint);
            Assert.NotNull(filter);
            Assert.IsType<AllActuatorsStartupFilter>(filter);
        }

        // TODO: test distributed tracing autowiring
        // TODO: test cf container id autowiring
        private readonly Action<IWebHostBuilder> _testServerWithRouting = builder =>
                        builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
    }
}
