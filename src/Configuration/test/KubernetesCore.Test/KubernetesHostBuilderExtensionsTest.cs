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
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test;

public class KubernetesHostBuilderExtensionsTest
{
    [Fact]
    public void AddKubernetesConfiguration_DefaultWebHost_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().UseStartup<TestServerStartup>();

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
        IApplicationInstanceInfo appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_WebHostBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IWebHostBuilder hostBuilder = new WebHostBuilder().UseStartup<TestServerStartup>();

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
        IApplicationInstanceInfo appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_DefaultHost_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder => builder.UseStartup<TestServerStartup>());

        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));
        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
        IApplicationInstanceInfo appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_HostBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        IHostBuilder hostBuilder = new HostBuilder().AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));

        IServiceProvider serviceProvider = hostBuilder.Build().Services;
        var config = serviceProvider.GetServices<IConfiguration>().SingleOrDefault() as ConfigurationRoot;
        IApplicationInstanceInfo appInfo = serviceProvider.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    [Fact]
    public void AddKubernetesConfiguration_WebApplicationBuilder_AddsConfig()
    {
        using var server = new MockKubeApiServer();
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.AddKubernetesConfiguration(GetFakeClientSetup(server.Uri.ToString()));

        WebApplication host = hostBuilder.Build();
        var config = host.Services.GetService<IConfiguration>() as IConfigurationRoot;
        IApplicationInstanceInfo appInfo = host.Services.GetServices<IApplicationInstanceInfo>().SingleOrDefault();

        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesConfigMapProvider))) == 2);
        Assert.True(config.Providers.Count(ics => ics.GetType().IsAssignableFrom(typeof(KubernetesSecretProvider))) == 2);
        Assert.IsAssignableFrom<KubernetesApplicationOptions>(appInfo);
    }

    private Action<KubernetesClientConfiguration> GetFakeClientSetup(string host)
    {
        return fakeClient =>
        {
            fakeClient.Namespace = "default";
            fakeClient.Host = host;
        };
    }
}
