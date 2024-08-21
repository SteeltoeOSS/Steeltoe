// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Discovery.HttpClients.Test;

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
    public void AddEurekaDiscoveryClient_IHostBuilder_AddsServiceDiscovery_Eureka()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureWebHost(builder => builder.UseTestServer());
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        hostBuilder.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        using IHost host = hostBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);

        DiscoveryClientHostedService? hostedService = host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().SingleOrDefault();
        Assert.NotNull(hostedService);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_IHostBuilder_StartsUp()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureWebHost(builder => builder.UseTestServer());
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        hostBuilder.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        await hostBuilder.StartAsync();

        Assert.True(true);
    }

    [Fact]
    public void AddConsulDiscoveryClient_IHostBuilder_AddsServiceDiscovery_Consul()
    {
        IHostBuilder hostBuilder = TestHostBuilderFactory.Create();
        hostBuilder.ConfigureWebHost(builder => builder.UseTestServer());
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        hostBuilder.ConfigureServices(services => services.AddConsulDiscoveryClient());

        using IHost host = hostBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);

        DiscoveryClientHostedService? hostedService = host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().SingleOrDefault();
        Assert.NotNull(hostedService);
    }
}
