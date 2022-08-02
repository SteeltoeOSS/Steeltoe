// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public class DiscoveryHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> EurekaSettings = new()
    {
        ["eureka:client:shouldRegister"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "0",
        ["eureka:client:eurekaServer:retryCount"] = "0"
    };

    private static readonly Dictionary<string, string> ConsulSettings = new()
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

        IHost host = hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        IHostedService hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.NotNull(hostedService);
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public async Task AddServiceDiscovery_IHostBuilder_StartsUp()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        await hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).StartAsync();

        Assert.True(true);
    }

    [Fact]
    public void AddServiceDiscovery_IHostBuilder_AddsServiceDiscovery_Consul()
    {
        IHostBuilder hostBuilder = new HostBuilder().ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        IHost host = hostBuilder.AddServiceDiscovery(builder => builder.UseConsul()).Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        IHostedService hostedService = host.Services.GetServices<IHostedService>().FirstOrDefault();

        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.NotNull(hostedService);
        Assert.IsType<DiscoveryClientService>(hostedService);
    }
}
