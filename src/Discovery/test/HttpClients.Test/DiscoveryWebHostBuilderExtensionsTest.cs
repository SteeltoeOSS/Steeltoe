// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Discovery.HttpClients.Test;

public sealed class DiscoveryWebHostBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> EurekaSettings = new()
    {
        ["eureka:client:shouldRegisterWithEureka"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "1",
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
    public void AddEurekaDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettings));

        hostBuilder.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        using IWebHost host = hostBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);

        Assert.Single(host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>());
    }

    [Fact]
    public void AddConsulDiscoveryClient_IWebHostBuilder_AddsServiceDiscovery_Consul()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        hostBuilder.ConfigureServices(services => services.AddConsulDiscoveryClient());

        using IWebHost host = hostBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);

        Assert.Single(host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>());
    }
}
