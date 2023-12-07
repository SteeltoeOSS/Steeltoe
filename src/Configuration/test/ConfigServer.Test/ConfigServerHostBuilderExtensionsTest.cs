// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions;
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
    private static readonly Action<IApplicationBuilder> EmptyConfigureApp = _ =>
    {
    };

    [Fact]
    public void AddConfigServer_DefaultWebHost_AddsConfigServer()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true);
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

    [Fact]
    public void AddConfigServer_HostBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
        };

        var hostBuilder = new HostBuilder();
        hostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        hostBuilder.AddConfigServer();

        ConfigServerConfigurationProvider provider;

        using (IHost host = hostBuilder.Build())
        {
            var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
            provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_WebHostBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
        };

        IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder();
        webHostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        webHostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        webHostBuilder.AddConfigServer();
        webHostBuilder.Configure(EmptyConfigureApp);

        ConfigServerConfigurationProvider provider;

        using (IWebHost webHost = webHostBuilder.Build())
        {
            var configurationRoot = (IConfigurationRoot)webHost.Services.GetRequiredService<IConfiguration>();
            provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_WebApplicationBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
        };

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();

        var configurationRoot = (IConfigurationRoot)hostBuilder.Configuration;
        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        using (WebApplication host = hostBuilder.Build())
        {
            _ = host.Services.GetRequiredService<IConfiguration>();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }
}
