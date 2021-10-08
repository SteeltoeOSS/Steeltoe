// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var hostBuilder = WebHost.CreateDefaultBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).UseStartup<TestConfigServerStartup>();

            hostBuilder.AddConfigServer();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void AddConfigServer_New_WebHostBuilder_AddsConfigServer()
        {
            var hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).UseStartup<TestConfigServerStartup>();

            hostBuilder.AddConfigServer();
            var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }

        [Fact]
        public void AddConfigServer_IHostBuilder_AddsConfigServer()
        {
            var hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(quickTests)).AddConfigServer();

            var host = hostBuilder.Build();
            var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

            Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
            Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
        }
    }
}
