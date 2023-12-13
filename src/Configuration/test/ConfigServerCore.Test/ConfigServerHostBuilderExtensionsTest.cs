// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test;

public class ConfigServerHostBuilderExtensionsTest
{
    private static readonly Action<IApplicationBuilder> EmptyConfigureApp = _ =>
    {
    };

    private readonly Dictionary<string, string> quickTests = new () { { "spring:cloud:config:timeout", "10" } };

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

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance) !;
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

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance) !;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddConfigServer();
        var host = hostBuilder.Build();

        var config = host.Services.GetService<IConfiguration>() as IConfigurationRoot;
        Assert.Single(config.Providers.OfType<CloudFoundryConfigurationProvider>());
        Assert.Single(config.Providers.OfType<ConfigServerConfigurationProvider>());
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

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance) !;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }
#endif
}