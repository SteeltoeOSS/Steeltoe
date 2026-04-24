// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Discovery.HttpClients.Test;

public sealed class DiscoveryHostApplicationBuilderExtensionsTest
{
    private static readonly Dictionary<string, string?> EurekaSettings = new()
    {
        ["eureka:client:shouldRegisterWithEureka"] = "true",
        ["eureka:client:eurekaServer:connectTimeoutSeconds"] = "0",
        ["eureka:client:eurekaServer:retryCount"] = "0"
    };

    private static readonly Dictionary<string, string?> ConsulSettings = new()
    {
        ["consul:discovery:serviceName"] = "test-host",
        ["consul:discovery:enabled"] = "true",
        ["consul:discovery:failFast"] = "false",
        ["consul:discovery:register"] = "false"
    };

    [Fact]
    public void AddEurekaDiscoveryClient_HostApplicationBuilder_AddsServiceDiscovery_Eureka()
    {
        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        hostBuilder.Services.AddEurekaDiscoveryClient();

        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should().BeOfType<EurekaDiscoveryClient>();
        host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_HostApplicationBuilder_StartsUp()
    {
        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(EurekaSettings);
        hostBuilder.Services.AddEurekaDiscoveryClient();

        using IHost host = hostBuilder.Build();

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await host.StartAsync(TestContext.Current.CancellationToken);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void AddConsulDiscoveryClient_HostApplicationBuilder_AddsServiceDiscovery_Consul()
    {
        HostApplicationBuilder hostBuilder = TestHostApplicationBuilderFactory.Create();
        hostBuilder.Configuration.AddInMemoryCollection(ConsulSettings);
        hostBuilder.Services.AddConsulDiscoveryClient();

        using IHost host = hostBuilder.Build();

        host.Services.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should().BeOfType<ConsulDiscoveryClient>();
        host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }
}
