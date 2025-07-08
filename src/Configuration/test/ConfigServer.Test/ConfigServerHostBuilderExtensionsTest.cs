// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHostBuilderExtensionsTest
{
    [Fact]
    public void AddConfigServer_WebHost_AddsConfigServer()
    {
        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.Add(FastTestConfigurations.ConfigServer));
        hostBuilder.AddConfigServer();
        using IWebHost host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    [Fact]
    public void AddConfigServer_IHostBuilder_AddsConfigServer()
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.Add(FastTestConfigurations.ConfigServer));
        hostBuilder.AddConfigServer();
        using IHost host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    [Fact]
    public void AddConfigServer_HostApplicationBuilder_AddsConfigServer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        };

        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        using IHost host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();

        configuration.EnumerateProviders<CloudFoundryConfigurationProvider>().Should().ContainSingle();
        configuration.EnumerateProviders<ConfigServerConfigurationProvider>().Should().ContainSingle();
    }

    [Fact]
    public void AddConfigServer_HostBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:pollingInterval"] = 1.Seconds().ToString()
        };

        HostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        hostBuilder.AddConfigServer();

        ConfigServerConfigurationProvider provider;

        using (IHost host = hostBuilder.Build())
        {
            var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
            provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_WebHostBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:pollingInterval"] = 1.Seconds().ToString()
        };

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
        builder.AddConfigServer();

        ConfigServerConfigurationProvider provider;

        using (IWebHost webHost = builder.Build())
        {
            var configurationRoot = (IConfigurationRoot)webHost.Services.GetRequiredService<IConfiguration>();
            provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:pollingInterval"] = 1.Seconds().ToString()
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();

        IConfigurationRoot configurationRoot = hostBuilder.Configuration;
        ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

        await using (WebApplication host = hostBuilder.Build())
        {
            _ = host.Services.GetRequiredService<IConfiguration>();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_HostApplicationBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:pollingInterval"] = 1.Seconds().ToString()
        };

        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();

        IConfigurationRoot configurationRoot = hostBuilder.Configuration;
        ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

        using (IHost host = hostBuilder.Build())
        {
            _ = host.Services.GetRequiredService<IConfiguration>();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromConfigServerConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:cloud:config:name"] = "myApp"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromSpringConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:application:name"] = "myApp"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromVcapApplicationConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["vcap:application:application_name"] = "myApp"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromHostingEnvironment()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        };

        var options = new WebApplicationOptions
        {
            ApplicationName = "myApp"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create(options);
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesEnvironmentNameFromConfigServerConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:cloud:config:env"] = "TestEnv"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Environment.Should().Be("TestEnv");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesEnvironmentNameFromHostingEnvironment()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.Environment.EnvironmentName = "TestEnv";
        hostBuilder.AddConfigServer();
        await using WebApplication host = hostBuilder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Environment.Should().Be("TestEnv");
    }
}
