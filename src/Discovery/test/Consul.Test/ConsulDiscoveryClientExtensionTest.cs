// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Consul.Discovery;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test;

public class ConsulDiscoveryClientExtensionTest
{
    [Fact]
    public void ClientEnabledByDefault()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        ConsulDiscoveryClientExtension.ConfigureConsulServices(services);
        var provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientDisabledBySpringCloudDiscoveryEnabledFalse()
    {
        var services = new ServiceCollection();
        var appSettings = new Dictionary<string, string> { { "spring:cloud:discovery:enabled", "false" } };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        ConsulDiscoveryClientExtension.ConfigureConsulServices(services);
        var provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.False(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientFavorsConsulDiscoveryEnabled()
    {
        var services = new ServiceCollection();
        var appSettings = new Dictionary<string, string>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "consul:discovery:enabled", "true" }
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        ConsulDiscoveryClientExtension.ConfigureConsulServices(services);
        var provider = services.BuildServiceProvider();
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }
}
