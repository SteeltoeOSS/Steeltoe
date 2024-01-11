// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.Test;

public sealed class KubernetesHostBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesConfiguration_DefaultWebHost_AddsConfig()
    {
        using var server = new MockKubeApiServer();

        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .UseStartup<TestServerStartup>();

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var configurationRoot = (ConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>();
        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_WebHostBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>();

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var configurationRoot = (ConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>();
        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_DefaultHost_AddsConfig()
    {
        using var server = new MockKubeApiServer();

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .ConfigureWebHostDefaults(builder => builder.UseStartup<TestServerStartup>());

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var configurationRoot = (ConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>();
        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_HostBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IHostBuilder hostBuilder = new HostBuilder().AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));

        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var configurationRoot = (ConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>();
        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();

        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_WebApplicationBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));

        WebApplication host = hostBuilder.Build();
        var configurationRoot = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
        var appInfo = host.Services.GetRequiredService<IApplicationInstanceInfo>();

        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(configurationRoot.Providers.Count(provider => provider.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    private static Action<KubernetesClientConfiguration> GetFakeClientSetup(string host)
    {
        return fakeClient =>
        {
            fakeClient.Namespace = "default";
            fakeClient.Host = host;
        };
    }
}
