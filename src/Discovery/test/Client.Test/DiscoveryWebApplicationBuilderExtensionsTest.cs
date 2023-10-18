// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class DiscoveryWebApplicationBuilderExtensionsTest
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
    public void AddDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        webApplicationBuilder.AddDiscoveryClient();
        WebApplication host = webApplicationBuilder.Build();

        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.Single(host.Services.GetServices<IHostedService>().Where(s => s is DiscoveryClientService));
    }

    [Fact]
    public void AddDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(ConsulSettings);
        webApplicationBuilder.AddDiscoveryClient();
        WebApplication host = webApplicationBuilder.Build();

        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.Single(host.Services.GetServices<IHostedService>().Where(s => s is DiscoveryClientService));
    }

    [Fact]
    public void AddServiceDiscovery_WebApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        webApplicationBuilder.AddServiceDiscovery(builder => builder.UseEureka());

        WebApplication host = webApplicationBuilder.Build();
        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        Assert.Single(discoveryClient);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClient.First());
        Assert.Single(host.Services.GetServices<IHostedService>().Where(s => s is DiscoveryClientService));
    }

    [Fact]
    public void AddServiceDiscovery_WebApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(ConsulSettings);
        webApplicationBuilder.AddServiceDiscovery(builder => builder.UseConsul());
        WebApplication host = webApplicationBuilder.Build();

        IEnumerable<IDiscoveryClient> discoveryClient = host.Services.GetServices<IDiscoveryClient>();
        Assert.Single(discoveryClient);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClient.First());
        Assert.Single(host.Services.GetServices<IHostedService>().Where(s => s is DiscoveryClientService));
    }
}
