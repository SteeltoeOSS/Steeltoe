// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Discovery.Client.Test
{
    public class DiscoveryHostBuilderExtensionsTest
    {
        private static readonly Dictionary<string, string> EurekaSettings = new Dictionary<string, string>()
        {
            ["eureka:client:shouldRegister"] = "true",
            ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "1",
            ["eureka:client:eurekaServer:retryCount"] = "0",
        };

        private static readonly Dictionary<string, string> ConsulSettings = new Dictionary<string, string>()
        {
            ["consul:discovery:serviceName"] = "testhost",
            ["consul:discovery:enabled"] = "true",
            ["consul:discovery:failfast"] = "false",
            ["consul:discovery:register"] = "false",
        };

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Eureka()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(EurekaSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
            Assert.NotNull(hostedService);
            Assert.IsType<DiscoveryClientService>(hostedService);
        }

        [Fact]
        public async Task AddServiceDiscovery_IHostBuilder_StartsUp()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(EurekaSettings));

            // Act
            var host = await hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).StartAsync();

            Assert.True(true);
        }

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Consul()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(ConsulSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery(builder => builder.UseConsul()).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
            Assert.NotNull(hostedService);
            Assert.IsType<DiscoveryClientService>(hostedService);
        }
    }
}
