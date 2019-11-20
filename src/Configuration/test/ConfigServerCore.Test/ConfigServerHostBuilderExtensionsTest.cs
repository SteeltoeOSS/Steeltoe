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
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerHostBuilderExtensionsTest
    {
        [Fact]
        public void AddConfigServer_DefaultWebHost_AddsConfigServer()
        {
            // Arrange
            var hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestConfigServerStartup>();

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
            var hostBuilder = new WebHostBuilder().UseStartup<TestConfigServerStartup>();

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
            var hostBuilder = new HostBuilder().AddConfigServer();

            // Act
            var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            // Assert
            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        [Obsolete]
        public void UseCloudFoundryHosting_ThrowsIfHostBuilderNull()
        {
            // Arrange
            IWebHostBuilder webHostBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerHostBuilderExtensions.UseCloudFoundryHosting(webHostBuilder));
            Assert.Contains(nameof(webHostBuilder), ex.Message);
        }

        [Fact]
        [Obsolete]
        public void UseCloudFoundryHosting_DoNotSetUrlsIfNull()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", null);
            var hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestConfigServerStartup>();

            // Act
            hostBuilder.UseCloudFoundryHosting();
            var server = new TestServer(hostBuilder);

            // Assert
            var addresses = server.Host.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Null(addresses);
        }

        [Fact]
        [Obsolete]
        public void UseCloudFoundryHosting_MakeSureThePortIsSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("PORT", "42");
            var hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestConfigServerStartup>();

            // Act
            hostBuilder.UseCloudFoundryHosting();
            var server = hostBuilder.Build();

            // Assert
            var addresses = server.ServerFeatures.Get<IServerAddressesFeature>();
            Assert.Contains("http://*:42", addresses.Addresses);
        }
    }
}
