// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.Client.Test;

public sealed class DiscoveryWebApplicationBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> EurekaSettings = new()
    {
        ["eureka:client:shouldRegister"] = "true",
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
    public void AddDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        webApplicationBuilder.Services.AddDiscoveryClient(webApplicationBuilder.Configuration);
        WebApplication host = webApplicationBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientService));
    }

    [Fact]
    public void AddDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder();
        webApplicationBuilder.Host.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        webApplicationBuilder.Configuration.AddInMemoryCollection(ConsulSettings);
        webApplicationBuilder.Services.AddDiscoveryClient(webApplicationBuilder.Configuration);
        WebApplication host = webApplicationBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientService));
    }

    [Fact]
    public void AddServiceDiscovery_WebApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        webApplicationBuilder.Services.AddServiceDiscovery(webApplicationBuilder.Configuration, builder => builder.UseEureka());

        WebApplication host = webApplicationBuilder.Build();
        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientService));
    }

    [Fact]
    public void AddServiceDiscovery_WebApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        WebApplicationBuilder webApplicationBuilder = TestHelpers.GetTestWebApplicationBuilder();
        webApplicationBuilder.Configuration.AddInMemoryCollection(ConsulSettings);
        webApplicationBuilder.Services.AddServiceDiscovery(webApplicationBuilder.Configuration, builder => builder.UseConsul());
        WebApplication host = webApplicationBuilder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientService));
    }
}
