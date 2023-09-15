// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHostBuilderExtensionsTest
{
    [Fact]
    public void AddConfigServer_DefaultWebHost_AddsConfigServer()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));
        hostBuilder.UseStartup<TestConfigServerStartup>();
        hostBuilder.AddConfigServer();

        IWebHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>());
        Assert.NotNull(configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_New_WebHostBuilder_AddsConfigServer()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));
        hostBuilder.UseStartup<TestConfigServerStartup>();
        hostBuilder.AddConfigServer();

        IWebHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>());
        Assert.NotNull(configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_IHostBuilder_AddsConfigServer()
    {
        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(TestHelpers.FastTestsConfiguration));
        hostBuilder.AddConfigServer();

        IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>());
        Assert.NotNull(configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddConfigServer();

        WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.NotNull(configuration.FindConfigurationProvider<CloudFoundryConfigurationProvider>());
        Assert.NotNull(configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>());
    }
}
