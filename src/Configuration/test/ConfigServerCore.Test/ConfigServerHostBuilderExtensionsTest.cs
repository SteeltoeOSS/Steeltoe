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

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerHostBuilderExtensionsTest
    {
        private readonly Dictionary<string, string> quickTests = new Dictionary<string, string> { { "spring:cloud:config:timeout", "10" } };

        [Fact]
        public void AddConfigServer_DefaultWebHost_AddsConfigServer()
        {
            // Arrange
            var hostBuilder = WebHost.CreateDefaultBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).UseStartup<TestConfigServerStartup>();

            // Act
            hostBuilder.AddConfigServer();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void AddConfigServer_New_WebHostBuilder_AddsConfigServer()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).UseStartup<TestConfigServerStartup>();

            // Act
            hostBuilder.AddConfigServer();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void AddConfigServer_IHostBuilder_AddsConfigServer()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).AddConfigServer();

            // Act
            var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }
    }
}
