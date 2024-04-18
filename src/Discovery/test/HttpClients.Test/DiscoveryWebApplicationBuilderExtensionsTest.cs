// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;
using Xunit;

namespace Steeltoe.Discovery.HttpClients.Test;

public sealed class DiscoveryWebApplicationBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> EurekaSettings = new()
    {
        ["eureka:client:enabled"] = "true",
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
    public void AddEurekaDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EurekaSettings);
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication host = builder.Build();
        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();

        Assert.Single(discoveryClients);
        Assert.IsType<EurekaDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientHostedService));
    }

    [Fact]
    public void AddConsulDiscoveryClient_WebApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(ConsulSettings);
        builder.Services.AddConsulDiscoveryClient();

        using WebApplication host = builder.Build();

        IDiscoveryClient[] discoveryClients = host.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(service => service is DiscoveryClientHostedService));
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_WorksWithGlobalServiceDiscovery()
    {
        WebApplicationBuilder builder = TestHelpers.GetTestWebApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(EurekaSettings);

        builder.Services.AddEurekaDiscoveryClient();
        builder.Services.ConfigureHttpClientDefaults(action => action.AddServiceDiscovery());

        await using WebApplication host = builder.Build();

        Task<EurekaDiscoveryClient> resolveTask = Task.Run(() => _ = host.Services.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().Single());
        Func<Task> action = async () => await resolveTask.WaitAsync(TimeSpan.FromSeconds(5));

        await action.Should().NotThrowAsync();
    }
}
