// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
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
        private static Dictionary<string, string> eurekaSettings = new Dictionary<string, string>()
        {
            ["eureka:client:shouldRegister"] = "true",
            ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "1",
            ["eureka:client:eurekaServer:retryCount"] = "0",
        };

        private static Dictionary<string, string> consulSettings = new Dictionary<string, string>()
        {
            ["consul:discovery:serviceName"] = "testhost",
            ["consul:discovery:enabled"] = "true",
            ["consul:discovery:failfast"] = "false",
        };

        [Fact]
        public void AddServiceDiscovery_IWebHostBuilder_AddsServiceDiscovery_Eureka()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().Configure(configure => { }).ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(eurekaSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery(options => options.UseEureka()).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var filters = host.Services.GetServices<IStartupFilter>();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
            Assert.NotEmpty(filters);
            Assert.Contains(filters, f => f.GetType() == typeof(DiscoveryClientStartupFilter));
        }

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Eureka()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(eurekaSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery(options => options.UseEureka()).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
            Assert.NotNull(filter);
            Assert.IsType<DiscoveryClientStartupFilter>(filter);
        }

        [Fact]
        public async Task AddServiceDiscovery_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(c => c.UseTestServer().Configure(app => { }))
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(eurekaSettings));

            // Act
            var host = await hostBuilder.AddServiceDiscovery(options => options.UseEureka()).StartAsync();

            // Assert general success...
            //   not sure how to specifically validate that the StartupFilter fired,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.True(true);
        }

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Consul()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(consulSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery(options => options.UseConsul()).Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
            Assert.NotNull(filter);
            Assert.IsType<DiscoveryClientStartupFilter>(filter);
        }
    }
}
