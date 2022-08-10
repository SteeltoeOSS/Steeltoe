// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerHostBuilderExtensionsTest
{
    [Fact]
    public void AddConfigServer_DefaultWebHost_AddsConfigServer()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration))
            .UseStartup<TestConfigServerStartup>();

        hostBuilder.AddConfigServer();
        var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_New_WebHostBuilder_AddsConfigServer()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration))
            .UseStartup<TestConfigServerStartup>();

        hostBuilder.AddConfigServer();
        var config = hostBuilder.Build().Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_IHostBuilder_AddsConfigServer()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration)).AddConfigServer();

        IHost host = hostBuilder.Build();
        var config = host.Services.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;

        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddConfigServer();
        WebApplication host = hostBuilder.Build();

        var config = host.Services.GetService<IConfiguration>() as IConfigurationRoot;
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
    }
}
