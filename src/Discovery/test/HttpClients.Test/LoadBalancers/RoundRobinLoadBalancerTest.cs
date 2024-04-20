// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.HttpClients.LoadBalancers;
using Xunit;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class RoundRobinLoadBalancerTest
{
    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);
        var loadBalancer = new RoundRobinLoadBalancer([client], null, null, NullLogger<RoundRobinLoadBalancer>.Instance);

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
        options.Services.RemoveAt(options.Services.Count - 1);

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri.Port.Should().Be(8010);
    }

    [Fact]
    public async Task ResolveServiceInstance_ResolvesAndIncrementsServiceIndex_WithDistributedCache()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);
        IDistributedCache distributedCache = GetCache();
        var loadBalancer = new RoundRobinLoadBalancer([client], distributedCache, null, NullLogger<RoundRobinLoadBalancer>.Instance);

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
        options.Services.RemoveAt(options.Services.Count - 1);
        await distributedCache.RemoveAsync("Steeltoe-LoadBalancerIndex-vegetableservice");

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetableservice/api"), CancellationToken.None);
        vegetableUri.Port.Should().Be(8010);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_NonDefaultPort_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var loadBalancer = new RoundRobinLoadBalancer([client], NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo:8080/test");

        Uri result = await loadBalancer.ResolveServiceInstanceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_DoesNotFindService_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var handler = new RoundRobinLoadBalancer([client], NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo/test");

        Uri result = await handler.ResolveServiceInstanceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_FindsService_ReturnsURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
        var handler = new RoundRobinLoadBalancer([client], NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

        Uri result = await handler.ResolveServiceInstanceAsync(uri, CancellationToken.None);
        Assert.Equal(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"), result);
    }

    private static ConfigurationDiscoveryOptions CreateTestServiceInstances()
    {
        return new ConfigurationDiscoveryOptions
        {
            Services =
            {
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
                    IsSecure = true,
                    Metadata =
                    {
                        ["name"] = "meta-vegemite"
                    }
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableservice",
                    Host = "carrot",
                    Port = 8011,
                    Metadata =
                    {
                        ["name"] = "meta-carrot"
                    }
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableservice",
                    Host = "beet",
                    Port = 8012,
                    Metadata =
                    {
                        ["name"] = "meta-beet"
                    }
                }
            }
        };
    }

    private static IDistributedCache GetCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        return serviceProvider.GetRequiredService<IDistributedCache>();
    }

    private sealed class TestServiceInstance : IServiceInstance
    {
        public string ServiceId => throw new NotImplementedException();
        public string Host => throw new NotImplementedException();
        public int Port => throw new NotImplementedException();
        public bool IsSecure => throw new NotImplementedException();
        public Uri Uri { get; }
        public IReadOnlyDictionary<string, string?> Metadata => throw new NotImplementedException();

        public TestServiceInstance(Uri uri)
        {
            Uri = uri;
        }
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
        private readonly IServiceInstance? _instance;

        public string Description => throw new NotImplementedException();

        public TestDiscoveryClient(IServiceInstance? instance = null)
        {
            _instance = instance;
        }

        public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
        {
            IList<IServiceInstance> instances = [];

            if (_instance != null)
            {
                instances.Add(_instance);
            }

            return Task.FromResult(instances);
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
