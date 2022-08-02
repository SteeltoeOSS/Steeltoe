// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Kubernetes.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Kubernetes.Test;

public class KubernetesDiscoveryClientExtensionTest
{
    [Fact]
    public void ClientEnabledByDefault()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        KubernetesDiscoveryClientExtension.ConfigureKubernetesServices(services);
        ServiceProvider? provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientDisabledBySpringCloudDiscoveryEnabledFalse()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        KubernetesDiscoveryClientExtension.ConfigureKubernetesServices(services);
        ServiceProvider? provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();

        Assert.False(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientFavorsKubernetesDiscoveryEnabled()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "spring:cloud:kubernetes:discovery:enabled", "true" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        KubernetesDiscoveryClientExtension.ConfigureKubernetesServices(services);
        ServiceProvider? provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }
}
