// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class RoundRobinLoadBalancerTest
{
    [Fact]
    public async Task ResolveServiceInstanceAsync_ResolvesAndIncrementsServiceIndex()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        var loadBalancer = new RoundRobinLoadBalancer(resolver, null, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruit-service/api"), TestContext.Current.CancellationToken);
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        Uri vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);

        fruitUri.Port.Should().Be(8000);
        vegetableUri.Port.Should().Be(8011);

        // wrap around
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);

        vegetableUri.Port.Should().Be(8010);

        // reset when service has disappeared
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        options.Services.RemoveAt(options.Services.Count - 1);

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        vegetableUri.Port.Should().Be(8010);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_ResolvesAndIncrementsServiceIndex_WithDistributedCache()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        IDistributedCache distributedCache = GetCache();
        var loadBalancer = new RoundRobinLoadBalancer(resolver, distributedCache, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruit-service/api"), TestContext.Current.CancellationToken);
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        Uri vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);

        fruitUri.Port.Should().Be(8000);
        vegetableUri.Port.Should().Be(8011);

        // wrap around
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);

        vegetableUri.Port.Should().Be(8010);

        // reset when service has disappeared
        _ = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        options.Services.RemoveAt(options.Services.Count - 1);
        await distributedCache.RemoveAsync("Steeltoe-LoadBalancerIndex-vegetable-service", TestContext.Current.CancellationToken);

        vegetableUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://vegetable-service/api"), TestContext.Current.CancellationToken);
        vegetableUri.Port.Should().Be(8010);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_NonDefaultPort_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        var loadBalancer = new RoundRobinLoadBalancer(resolver, NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo:8080/test");

        Uri result = await loadBalancer.ResolveServiceInstanceAsync(uri, TestContext.Current.CancellationToken);
        result.Should().Be(uri);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_DoesNotFindService_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        var handler = new RoundRobinLoadBalancer(resolver, NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo/test");

        Uri result = await handler.ResolveServiceInstanceAsync(uri, TestContext.Current.CancellationToken);
        result.Should().Be(uri);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_FindsService_ReturnsURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        var handler = new RoundRobinLoadBalancer(resolver, NullLogger<RoundRobinLoadBalancer>.Instance);
        var uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

        Uri result = await handler.ResolveServiceInstanceAsync(uri, TestContext.Current.CancellationToken);
        result.Should().Be(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"));
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_SkipsOverThrowingDiscoveryClients()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);

        IDiscoveryClient[] clients =
        [
            new ThrowingDiscoveryClient(),
            new ConfigurationDiscoveryClient(optionsMonitor)
        ];

        var resolver = new ServiceInstancesResolver(clients, NullLogger<ServiceInstancesResolver>.Instance);
        var loadBalancer = new RoundRobinLoadBalancer(resolver, null, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruit-service/api"), TestContext.Current.CancellationToken);
        fruitUri.Should().Be("https://fruit-ball:8000/api");
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_CachesInstances()
    {
        ConfigurationDiscoveryOptions options = CreateTestServiceInstances();
        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);
        IDistributedCache distributedCache = GetCache();
        var resolver = new ServiceInstancesResolver([client], distributedCache, null, NullLogger<ServiceInstancesResolver>.Instance);
        var loadBalancer = new RoundRobinLoadBalancer(resolver, null, null, NullLogger<RoundRobinLoadBalancer>.Instance);

        Uri fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruit-service/api"), TestContext.Current.CancellationToken);
        fruitUri.Should().Be("https://fruit-ball:8000/api");

        optionsMonitor.Change(new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruit-service",
                    Host = "CHANGED",
                    Port = 8000,
                    IsSecure = true
                }
            }
        });

        fruitUri = await loadBalancer.ResolveServiceInstanceAsync(new Uri("https://fruit-service/api"), TestContext.Current.CancellationToken);
        fruitUri.Should().Be("https://fruit-ball:8000/api");
    }

    private static ConfigurationDiscoveryOptions CreateTestServiceInstances()
    {
        return new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruit-service",
                    Host = "fruit-ball",
                    Port = 8000,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruit-service",
                    Host = "fruit-ball",
                    Port = 8000,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruit-service",
                    Host = "fruit-baller",
                    Port = 8001
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruit-service",
                    Host = "fruit-ballers",
                    Port = 8002
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetable-service",
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
                    ServiceId = "vegetable-service",
                    Host = "carrot",
                    Port = 8011,
                    Metadata =
                    {
                        ["name"] = "meta-carrot"
                    }
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetable-service",
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

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        return serviceProvider.GetRequiredService<IDistributedCache>();
    }

    private sealed class TestServiceInstance(Uri uri) : IServiceInstance
    {
        public string ServiceId => throw new NotImplementedException();
        public string InstanceId => throw new NotImplementedException();
        public string Host => throw new NotImplementedException();
        public int Port => throw new NotImplementedException();
        public bool IsSecure => throw new NotImplementedException();
        public Uri Uri { get; } = uri;
        public Uri NonSecureUri => throw new NotImplementedException();
        public Uri SecureUri => throw new NotImplementedException();
        public IReadOnlyDictionary<string, string?> Metadata => throw new NotImplementedException();
    }

    private sealed class TestDiscoveryClient(IServiceInstance? instance = null) : IDiscoveryClient
    {
        private readonly IServiceInstance? _instance = instance;

        public string Description => throw new NotImplementedException();

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

    private sealed class ThrowingDiscoveryClient : IDiscoveryClient
    {
        public string Description => throw new NotImplementedException();

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new InvalidOperationException();
        }

        public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException();
        }

        public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException();
        }
    }
}
