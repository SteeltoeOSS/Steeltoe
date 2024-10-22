// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
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
        hostBuilder.UseStartup<TestConfigServerStartup>();
        hostBuilder.AddConfigServer();

        using IWebHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configuration.EnumerateProviders<CloudFoundryConfigurationProvider>());
        Assert.Single(configuration.EnumerateProviders<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_IHostBuilder_AddsConfigServer()
    {
        HostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.Add(FastTestConfigurations.ConfigServer));
        hostBuilder.AddConfigServer();

        using IHost host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configuration.EnumerateProviders<CloudFoundryConfigurationProvider>());
        Assert.Single(configuration.EnumerateProviders<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_AddsConfigServer()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.Single(configuration.EnumerateProviders<CloudFoundryConfigurationProvider>());
        Assert.Single(configuration.EnumerateProviders<ConfigServerConfigurationProvider>());
    }

    [Fact]
    public void AddConfigServer_HostBuilder_DisposesTimer()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
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
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
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
            ["spring:cloud:config:pollingInterval"] = TimeSpan.FromSeconds(1).ToString()
        };

        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddConfigServer();

        var configurationRoot = (IConfigurationRoot)hostBuilder.Configuration;
        ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();

        await using (WebApplication host = hostBuilder.Build())
        {
            _ = host.Services.GetRequiredService<IConfiguration>();
        }

        FieldInfo refreshTimerField = provider.GetType().GetField("_refreshTimer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        refreshTimerField.GetValue(provider).Should().BeNull();
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromConfigServerConfiguration()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:cloud:config:name"] = "myApp"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromSpringConfiguration()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:application:name"] = "myApp"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromVcapApplicationConfiguration()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["vcap:application:application_name"] = "myApp"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesAppNameFromHostingEnvironment()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create(new WebApplicationOptions
        {
            ApplicationName = "myApp"
        });

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Name.Should().Be("myApp");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesEnvironmentNameFromConfigServerConfiguration()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false",
            ["spring:cloud:config:env"] = "TestEnv"
        });

        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Environment.Should().Be("TestEnv");
    }

    [Fact]
    public async Task AddConfigServer_WebApplicationBuilder_TakesEnvironmentNameFromHostingEnvironment()
    {
        WebApplicationBuilder hostBuilder = TestWebApplicationBuilderFactory.Create();

        hostBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["spring:cloud:config:enabled"] = "false"
        });

        hostBuilder.Environment.EnvironmentName = "TestEnv";
        hostBuilder.AddConfigServer();

        await using WebApplication host = hostBuilder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        ConfigServerConfigurationProvider? provider = configuration.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        provider.Should().NotBeNull();
        provider!.ClientOptions.Environment.Should().Be("TestEnv");
    }
}
