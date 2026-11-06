// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class DiscoveryHttpClientBuilderExtensionsTest
{
    [Fact]
    public async Task AddServiceDiscovery_WithRandomLoadBalancer_AddsRandomLoadBalancerToServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddConfigurationDiscoveryClient();
        services.AddHttpClient("test").AddServiceDiscovery<RandomLoadBalancer>();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(RandomLoadBalancer)).Which.Lifetime.Should()
            .Be(ServiceLifetime.Singleton);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<RandomLoadBalancer>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddServiceDiscovery_WithAddRoundRobinLoadBalancer_AddsRoundRobinLoadBalancerToServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddConfigurationDiscoveryClient();
        services.AddHttpClient("test").AddServiceDiscovery<RoundRobinLoadBalancer>();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(RoundRobinLoadBalancer)).Which.Lifetime.Should()
            .Be(ServiceLifetime.Singleton);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<RoundRobinLoadBalancer>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddServiceDiscovery_WithoutLoadBalancer_AddsRandomLoadBalancerToServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddConfigurationDiscoveryClient();
        services.AddHttpClient("test").AddServiceDiscovery();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(RandomLoadBalancer)).Which.Lifetime.Should()
            .Be(ServiceLifetime.Singleton);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<RandomLoadBalancer>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddLoadBalancerT_DoesNotAddT_ToServices()
    {
        var services = new ServiceCollection();
        services.AddHttpClient("test").AddServiceDiscovery<FakeLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<FakeLoadBalancer>().Should().BeEmpty();
    }

    [Fact]
    public async Task AddLoadBalancerT_CanBeUsedWithAnHttpClient()
    {
        var loadBalancer = new FakeLoadBalancer();

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest resolvedRequest = handler.Mock.When(HttpMethod.Get, "https://some-resolved-host:1234/api").Respond(HttpStatusCode.OK);
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        var services = new ServiceCollection();
        services.DisableHttpClientRotationTimers();
        services.AddSingleton(loadBalancer);

        // ReSharper disable once AccessToDisposedClosure
        services.AddHttpClient("test").ConfigurePrimaryHttpMessageHandler(() => handler).AddServiceDiscovery<FakeLoadBalancer>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using HttpClient client = factory.CreateClient("test");

        using HttpResponseMessage response = await client.GetAsync(new Uri("https://replace-me/api"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Mock.GetMatchCount(resolvedRequest).Should().Be(1);
        loadBalancer.Statistics.Should().ContainSingle();
    }

    [Fact]
    public async Task CanAddMultipleLoadBalancers()
    {
        var services = new ServiceCollection();
        services.DisableHttpClientRotationTimers();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
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

        randomLbClient.Should().NotBeNull();
        randomLbClient2.Should().NotBeNull();
        roundRobinLbClient.Should().NotBeNull();
        roundRobinLbClient2.Should().NotBeNull();
        fakeLbClient.Should().NotBeNull();
        fakeLbClient2.Should().NotBeNull();
    }
}
