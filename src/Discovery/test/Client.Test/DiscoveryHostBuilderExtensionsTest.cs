// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class DiscoveryHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> EurekaSettings = new()
    {
        ["eureka:client:shouldRegister"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "0",
        ["eureka:client:eurekaServer:retryCount"] = "0"
    };

    private static readonly Dictionary<string, string?> ConsulSettings = new()
    {
        ["consul:discovery:serviceName"] = "testhost",
        ["consul:discovery:enabled"] = "true",
        ["consul:discovery:failfast"] = "false",
        ["consul:discovery:register"] = "false"
    };

    [Fact]
    public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Eureka()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));
        hostBuilder.ConfigureServices((context, services) => services.AddServiceDiscovery(context.Configuration, builder => builder.UseEureka()));

        IHost host = hostBuilder.Build();
        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        IHostedService? hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
        Assert.NotNull(hostedService);
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public async Task AddServiceDiscovery_IHostBuilder_StartsUp()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));
        hostBuilder.ConfigureServices((context, services) => services.AddServiceDiscovery(context.Configuration, builder => builder.UseEureka()));

        await hostBuilder.StartAsync();

        Assert.True(true);
    }

    [Fact]
    public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Consul()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));
        hostBuilder.ConfigureServices((context, services) => services.AddServiceDiscovery(context.Configuration, builder => builder.UseConsul()));

        IHost host = hostBuilder.Build();
        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        IHostedService? hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);
        Assert.NotNull(hostedService);
        Assert.IsType<DiscoveryClientService>(hostedService);
    }
}
