// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Common.Test.LoadBalancer;

public sealed class RoundRobinLoadBalancerTest
{
    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex()
    {
        List<ConfigurationServiceInstance> serviceInstances = CreateTestServiceInstances();

        var optionsMonitor = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(serviceInstances);
        var provider = new ConfigurationServiceInstanceProvider(optionsMonitor);
        var loadBalancer = new RoundRobinLoadBalancer(provider, null, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruitservice/api"), CancellationToken.None);
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        Uri vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        fruitUri.Port.Should().Be(8000);
        vegetableUri.Port.Should().Be(8011);

        // wrap around
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        vegetableUri.Port.Should().Be(8010);

        // reset when service has disappeared
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        serviceInstances.RemoveAt(serviceInstances.Count - 1);

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri.Port.Should().Be(8010);
    }

    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex_WithDistributedCache()
    {
        List<ConfigurationServiceInstance> serviceInstances = CreateTestServiceInstances();

        var optionsMonitor = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(serviceInstances);
        var provider = new ConfigurationServiceInstanceProvider(optionsMonitor);
        IDistributedCache distributedCache = GetCache();
        var loadBalancer = new RoundRobinLoadBalancer(provider, distributedCache, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruitservice/api"), CancellationToken.None);
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        Uri vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        fruitUri.Port.Should().Be(8000);
        vegetableUri.Port.Should().Be(8011);

        // wrap around
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        vegetableUri.Port.Should().Be(8010);

        // reset when service has disappeared
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        serviceInstances.RemoveAt(serviceInstances.Count - 1);
        await distributedCache.RemoveAsync("Steeltoe-LoadBalancerIndex-vegetableservice");

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri.Port.Should().Be(8010);
    }

    private static List<ConfigurationServiceInstance> CreateTestServiceInstances()
    {
        return
        [
            new ConfigurationServiceInstance
            {
                ServiceId = "fruitservice",
                Host = "fruitball",
                Port = 8000,
                IsSecure = true
            },
            new ConfigurationServiceInstance
            {
                ServiceId = "fruitservice",
                Host = "fruitballer",
                Port = 8001
            },
            new ConfigurationServiceInstance
            {
                ServiceId = "fruitservice",
                Host = "fruitballerz",
                Port = 8002
            },
            new ConfigurationServiceInstance
            {
                ServiceId = "vegetableservice",
                Host = "vegemite",
                Port = 8010,
                IsSecure = true
            },
            new ConfigurationServiceInstance
            {
                ServiceId = "vegetableservice",
                Host = "carrot",
                Port = 8011
            },
            new ConfigurationServiceInstance
            {
                ServiceId = "vegetableservice",
                Host = "beet",
                Port = 8012
            }
        ];
    }

    private static IDistributedCache GetCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        return serviceProvider.GetRequiredService<IDistributedCache>();
    }
}
