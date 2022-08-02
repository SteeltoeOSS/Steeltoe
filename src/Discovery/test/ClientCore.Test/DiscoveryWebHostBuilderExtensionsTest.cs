// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public class DiscoveryWebHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> EurekaSettings = new()
    {
        ["eureka:client:shouldRegister"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "1",
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
    public void AddDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        IWebHost host = hostBuilder.AddDiscoveryClient().Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Consul()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        IWebHost host = hostBuilder.AddDiscoveryClient().Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddServiceDiscovery_IWebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        IWebHost host = hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddServiceDiscovery_IWebHostBuilder_AddsServiceDiscovery_Consul()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        IWebHost host = hostBuilder.AddServiceDiscovery(builder => builder.UseConsul()).Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }
}
