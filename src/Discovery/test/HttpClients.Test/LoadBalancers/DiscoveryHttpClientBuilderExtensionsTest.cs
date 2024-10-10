// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class DiscoveryHttpClientBuilderExtensionsTest
{
    [Fact]
    public async Task AddServiceDiscovery_WithRandomLoadBalancer_AddsRandomLoadBalancerToServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();

        services.AddHttpClient("test").AddServiceDiscovery<RandomLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        ServiceDescriptor? serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType == typeof(RandomLoadBalancer));

        Assert.Single(serviceProvider.GetServices<RandomLoadBalancer>());

        Assert.NotNull(serviceEntryInCollection);
        Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
    }

    [Fact]
    public async Task AddServiceDiscovery_WithAddRoundRobinLoadBalancer_AddsRoundRobinLoadBalancerToServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();

        services.AddHttpClient("test").AddServiceDiscovery<RoundRobinLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        ServiceDescriptor? serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType == typeof(RoundRobinLoadBalancer));

        Assert.Single(serviceProvider.GetServices<RoundRobinLoadBalancer>());

        Assert.NotNull(serviceEntryInCollection);
        Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
    }

    [Fact]
    public async Task AddServiceDiscovery_WithoutLoadBalancer_AddsRandomLoadBalancerToServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();
        services.AddHttpClient("test").AddServiceDiscovery();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        ServiceDescriptor? serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType == typeof(RandomLoadBalancer));

        Assert.Single(serviceProvider.GetServices<RandomLoadBalancer>());

        Assert.NotNull(serviceEntryInCollection);
        Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
    }

    [Fact]
    public async Task AddLoadBalancerT_DoesNotAddT_ToServices()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery<FakeLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.Empty(serviceProvider.GetServices<FakeLoadBalancer>());
    }

    [Fact]
    public async Task AddLoadBalancerT_CanBeUsedWithAnHttpClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FakeLoadBalancer>();
        services.AddHttpClient("test").AddServiceDiscovery<FakeLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("test");

        Assert.NotNull(client);
    }

    [Fact]
    public async Task CanAddMultipleLoadBalancers()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();
        services.AddSingleton<FakeLoadBalancer>();

        services.AddHttpClient("testRandom").AddServiceDiscovery<RandomLoadBalancer>();
        services.AddHttpClient("testRandom2").AddServiceDiscovery<RandomLoadBalancer>();
        services.AddHttpClient("testRoundRobin").AddServiceDiscovery<RoundRobinLoadBalancer>();
        services.AddHttpClient("testRoundRobin2").AddServiceDiscovery<RoundRobinLoadBalancer>();
        services.AddHttpClient("testFake").AddServiceDiscovery<FakeLoadBalancer>();
        services.AddHttpClient("testFake2").AddServiceDiscovery<FakeLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        using HttpClient randomLbClient = factory.CreateClient("testRandom");
        using HttpClient randomLbClient2 = factory.CreateClient("testRandom2");
        using HttpClient roundRobinLbClient = factory.CreateClient("testRoundRobin");
        using HttpClient roundRobinLbClient2 = factory.CreateClient("testRoundRobin2");
        using HttpClient fakeLbClient = factory.CreateClient("testFake");
        using HttpClient fakeLbClient2 = factory.CreateClient("testFake2");

        Assert.NotNull(randomLbClient);
        Assert.NotNull(randomLbClient2);
        Assert.NotNull(roundRobinLbClient);
        Assert.NotNull(roundRobinLbClient2);
        Assert.NotNull(fakeLbClient);
        Assert.NotNull(fakeLbClient2);
    }
}
