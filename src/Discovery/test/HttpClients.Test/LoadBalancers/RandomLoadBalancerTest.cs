// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

public sealed class RandomLoadBalancerTest
{
    [Fact]
    public async Task ResolveServiceInstanceAsync_DoesNotFindService_ReturnsOriginalURI()
    {
        var options = new ConfigurationDiscoveryOptions();
        using var client = new ConfigurationDiscoveryClient(TestOptionsMonitor.Create(options));
        RandomLoadBalancer loadBalancer = CreateLoadBalancer(client);
        var uri = new Uri("https://foo/test");

        Uri result = await loadBalancer.ResolveServiceInstanceAsync(uri, TestContext.Current.CancellationToken);

        result.Should().Be(uri);
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_FindsService_ReturnsURI()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                CreateServiceInstance("foo", "foundit", 5555)
            }
        };

        using var client = new ConfigurationDiscoveryClient(TestOptionsMonitor.Create(options));
        RandomLoadBalancer loadBalancer = CreateLoadBalancer(client);
        var uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

        Uri result = await loadBalancer.ResolveServiceInstanceAsync(uri, TestContext.Current.CancellationToken);

        result.Should().Be(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"));
    }

    [Fact]
    public async Task ResolveServiceInstanceAsync_PicksFromAllInstances()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                CreateServiceInstance("svc", "host-a", 1000),
                CreateServiceInstance("svc", "host-b", 2000),
                CreateServiceInstance("svc", "host-c", 3000)
            }
        };

        using var client = new ConfigurationDiscoveryClient(TestOptionsMonitor.Create(options));
        RandomLoadBalancer loadBalancer = CreateLoadBalancer(client);
        var resultUris = new HashSet<Uri>();

        // The selection is random, so we cannot know which instance comes next. With 200 draws, the chance that an instance is never picked is below 1e-34.
        for (int index = 0; index < 200; index++)
        {
            var requestUri = new Uri("https://svc/api");
            Uri resultUri = await loadBalancer.ResolveServiceInstanceAsync(requestUri, TestContext.Current.CancellationToken);
            resultUris.Add(resultUri);
        }

        resultUris.Should().BeEquivalentTo([
            new Uri("https://host-a:1000/api"),
            new Uri("https://host-b:2000/api"),
            new Uri("https://host-c:3000/api")
        ]);
    }

    private static ConfigurationServiceInstance CreateServiceInstance(string serviceId, string host, int port)
    {
        return new ConfigurationServiceInstance
        {
            ServiceId = serviceId,
            Host = host,
            Port = port,
            IsSecure = true
        };
    }

    private static RandomLoadBalancer CreateLoadBalancer(ConfigurationDiscoveryClient client)
    {
        var resolver = new ServiceInstancesResolver([client], NullLogger<ServiceInstancesResolver>.Instance);
        return new RandomLoadBalancer(resolver, NullLogger<RandomLoadBalancer>.Instance);
    }
}
