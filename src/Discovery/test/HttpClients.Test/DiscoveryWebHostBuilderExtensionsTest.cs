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
    // Keeps the client enabled, so its startup and shutdown are exercised, but makes no network calls (like ConsulSettings does for Consul).
    private static readonly Dictionary<string, string?> EurekaSettingsWithoutNetworkCalls = new()
    {
        ["eureka:client:enabled"] = "true",
        ["eureka:client:shouldRegisterWithEureka"] = "false",
        ["eureka:client:shouldFetchRegistry"] = "false"
    };

    private static readonly Dictionary<string, string?> ConsulSettings = new()
    {
        ["consul:discovery:serviceName"] = "test-host",
        ["consul:discovery:enabled"] = "true",
        ["consul:discovery:failFast"] = "false",
        ["consul:discovery:register"] = "false"
    };

    [Fact]
    public async Task AddEurekaDiscoveryClient_WebHostBuilder_AddsServiceDiscovery_Eureka()
    {
        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(EurekaSettingsWithoutNetworkCalls));

        hostBuilder.ConfigureServices(services => services.AddEurekaDiscoveryClient());

        await using HostWrapper host = HostWrapper.Wrap(hostBuilder.Build());
        await host.StartAsync(TestContext.Current.CancellationToken);

        host.Services.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should().BeOfType<EurekaDiscoveryClient>();
        host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }

    [Fact]
    public void AddConsulDiscoveryClient_WebHostBuilder_AddsServiceDiscovery_Consul()
    {
        WebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(ConsulSettings));

        hostBuilder.ConfigureServices(services => services.AddConsulDiscoveryClient());

        using IWebHost host = hostBuilder.Build();

        host.Services.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should().BeOfType<ConsulDiscoveryClient>();
        host.Services.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }
}
