// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Common.Test.LoadBalancer;

public sealed class RoundRobinLoadBalancerTest
{
    [Fact]
    public void Throws_If_IServiceInstanceProviderNotProvided()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new RoundRobinLoadBalancer(null));
        Assert.Equal("serviceInstanceProvider", exception.ParamName);
    }

    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex()
    {
        var services = new List<ConfigurationServiceInstance>
        {
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitball",
                Port = 8000,
                IsSecure = true
            },
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitballer",
                Port = 8001
            },
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitballerz",
                Port = 8002
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "vegemite",
                Port = 8010,
                IsSecure = true
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "carrot",
                Port = 8011
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "beet",
                Port = 8012
            }
        };

        var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);
        var provider = new ConfigurationServiceInstanceProvider(serviceOptions);
        var loadBalancer = new RoundRobinLoadBalancer(provider);

        Assert.Throws<KeyNotFoundException>(() => loadBalancer.NextIndexForService[$"{RoundRobinLoadBalancer.IndexKeyPrefix}fruitService"]);
        Assert.Throws<KeyNotFoundException>(() => loadBalancer.NextIndexForService[$"{RoundRobinLoadBalancer.IndexKeyPrefix}vegetableService"]);

        Uri fruitResult = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruitservice/api"), CancellationToken.None);
        await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        Uri vegResult = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        Assert.Equal(1, loadBalancer.NextIndexForService[$"{RoundRobinLoadBalancer.IndexKeyPrefix}fruitservice"]);
        Assert.Equal(8000, fruitResult.Port);
        Assert.Equal(2, loadBalancer.NextIndexForService[$"{RoundRobinLoadBalancer.IndexKeyPrefix}vegetableservice"]);
        Assert.Equal(8011, vegResult.Port);
    }

    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex_WithDistributedCache()
    {
        var services = new List<ConfigurationServiceInstance>
        {
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitball",
                Port = 8000,
                IsSecure = true
            },
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitballer",
                Port = 8001
            },
            new()
            {
                ServiceId = "fruitservice",
                Host = "fruitballerz",
                Port = 8002
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "vegemite",
                Port = 8010,
                IsSecure = true
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "carrot",
                Port = 8011
            },
            new()
            {
                ServiceId = "vegetableservice",
                Host = "beet",
                Port = 8012
            }
        };

        var serviceOptions = new TestOptionsMonitor<List<ConfigurationServiceInstance>>(services);
        var provider = new ConfigurationServiceInstanceProvider(serviceOptions);
        var loadBalancer = new RoundRobinLoadBalancer(provider, GetCache());

        Uri fruitResult = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruitservice/api"), CancellationToken.None);
        await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        Uri vegResult = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);

        Assert.Equal(8000, fruitResult.Port);
        Assert.Equal(8011, vegResult.Port);
    }

    private IDistributedCache GetCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        return serviceProvider.GetService<IDistributedCache>();
    }
}
