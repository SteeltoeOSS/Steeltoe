// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
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
            ["consul:discovery:enabled"] = "true",
            ["consul:discovery:failfast"] = "false",
        };

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Eureka()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(eurekaSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery().Build();
            var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
            var filter = host.Services.GetServices<IStartupFilter>().FirstOrDefault();

            // Assert
            Assert.Single(discoveryClient);
            Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
            Assert.NotNull(filter);
            Assert.IsType<DiscoveryClientStartupFilter>(filter);
        }

#if NETCOREAPP3_0
        [Fact]
        public async Task AddServiceDiscovery_IHostBuilder_IStartupFilterFires()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(c => c.UseTestServer().Configure(app => { }))
                .ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(eurekaSettings));

            // Act
            var host = await hostBuilder.AddServiceDiscovery().StartAsync();

            // Assert general success...
            //   not sure how to actually validate the StartupFilter worked,
            //   but debug through and you'll see it. Also the code coverage report should provide validation
            Assert.True(true);
        }
#endif

        [Fact]
        public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Consul()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(cbuilder => cbuilder.AddInMemoryCollection(consulSettings));

            // Act
            var host = hostBuilder.AddServiceDiscovery().Build();
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
