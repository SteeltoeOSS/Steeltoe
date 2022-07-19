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
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public class DiscoveryWebHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string> EurekaSettings = new ()
    {
        ["eureka:client:shouldRegister"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "1",
        ["eureka:client:eurekaServer:retryCount"] = "0",
    };

    private static readonly Dictionary<string, string> ConsulSettings = new ()
    {
        ["consul:discovery:serviceName"] = "testhost",
        ["consul:discovery:enabled"] = "true",
        ["consul:discovery:failfast"] = "false",
        ["consul:discovery:register"] = "false",
    };

    [Fact]
    public void AddDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        var hostBuilder = new WebHostBuilder().Configure(_ => { }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        var host = hostBuilder.AddDiscoveryClient().Build();
        var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Consul()
    {
        var hostBuilder = new WebHostBuilder().Configure(_ => { }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        var host = hostBuilder.AddDiscoveryClient().Build();
        var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddServiceDiscovery_IWebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        var hostBuilder = new WebHostBuilder().Configure(_ => { }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        var host = hostBuilder.AddServiceDiscovery(builder => builder.UseEureka()).Build();
        var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }

    [Fact]
    public void AddServiceDiscovery_IWebHostBuilder_AddsServiceDiscovery_Consul()
    {
        var hostBuilder = new WebHostBuilder().Configure(_ => { }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        var host = hostBuilder.AddServiceDiscovery(builder => builder.UseConsul()).Build();
        var discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        var hostedService = host.Services.GetService<IHostedService>();

        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.IsType<DiscoveryClientService>(hostedService);
    }
}
