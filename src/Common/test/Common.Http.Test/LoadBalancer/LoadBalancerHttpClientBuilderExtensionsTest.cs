// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Http.LoadBalancer;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery.Client;
using Xunit;

namespace Steeltoe.Common.Http.Test.LoadBalancer;

public sealed class LoadBalancerHttpClientBuilderExtensionsTest
{
    [Fact]
    public void AddRandomLoadBalancer_AddsRandomLoadBalancerToServices()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());

        services.AddHttpClient("test").AddRandomLoadBalancer();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        ServiceDescriptor serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType == typeof(RandomLoadBalancer));

        Assert.Single(serviceProvider.GetServices<RandomLoadBalancer>());
        Assert.NotNull(serviceEntryInCollection);
        Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
    }

    [Fact]
    public void AddRoundRobinLoadBalancer_AddsRoundRobinLoadBalancerToServices()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());

        services.AddHttpClient("test").AddRoundRobinLoadBalancer();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        ServiceDescriptor serviceEntryInCollection = services.FirstOrDefault(service => service.ServiceType == typeof(RoundRobinLoadBalancer));

        Assert.Single(serviceProvider.GetServices<RoundRobinLoadBalancer>());
        Assert.Equal(ServiceLifetime.Singleton, serviceEntryInCollection.Lifetime);
    }

    [Fact]
    public void AddLoadBalancerT_DoesNotAddT_ToServices()
    {
        var services = new ServiceCollection();

        services.AddHttpClient("test").AddLoadBalancer<FakeLoadBalancer>();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.Empty(serviceProvider.GetServices<FakeLoadBalancer>());
    }

    [Fact]
    public void AddLoadBalancerT_CanBeUsedWithAnHttpClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(FakeLoadBalancer));

        services.AddHttpClient("test").AddLoadBalancer<FakeLoadBalancer>();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        HttpClient client = factory.CreateClient("test");

        Assert.NotNull(client);
    }

    [Fact]
    public void CanAddMultipleLoadBalancers()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDiscoveryClient(new ConfigurationBuilder().Build());
        services.AddSingleton(typeof(FakeLoadBalancer));

        services.AddHttpClient("testRandom").AddRandomLoadBalancer();
        services.AddHttpClient("testRandom2").AddRandomLoadBalancer();
        services.AddHttpClient("testRoundRobin").AddRoundRobinLoadBalancer();
        services.AddHttpClient("testRoundRobin2").AddRoundRobinLoadBalancer();
        services.AddHttpClient("testFake").AddLoadBalancer<FakeLoadBalancer>();
        services.AddHttpClient("testFake2").AddLoadBalancer<FakeLoadBalancer>();
        var factory = services.BuildServiceProvider(true).GetRequiredService<IHttpClientFactory>();
        HttpClient randomLbClient = factory.CreateClient("testRandom");
        HttpClient randomLbClient2 = factory.CreateClient("testRandom2");
        HttpClient roundRobinLbClient = factory.CreateClient("testRoundRobin");
        HttpClient roundRobinLbClient2 = factory.CreateClient("testRoundRobin2");
        HttpClient fakeLbClient = factory.CreateClient("testFake");
        HttpClient fakeLbClient2 = factory.CreateClient("testFake2");

        Assert.NotNull(randomLbClient);
        Assert.NotNull(randomLbClient2);
        Assert.NotNull(roundRobinLbClient);
        Assert.NotNull(roundRobinLbClient2);
        Assert.NotNull(fakeLbClient);
        Assert.NotNull(fakeLbClient2);
    }
}
