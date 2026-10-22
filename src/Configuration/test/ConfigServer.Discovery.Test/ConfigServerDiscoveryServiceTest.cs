// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Configuration.ConfigServer.Discovery.Test;

public sealed class ConfigServerDiscoveryServiceTest
{
    [Fact]
    public async Task ConfigServerDiscoveryService_FindsDiscoveryClients()
    {
        using ConfigurationRoot configurationRoot =
            new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().HaveCount(3);
        service.DiscoveryClients.OfType<ConfigurationDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<ConsulDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<EurekaDiscoveryClient>().Should().ContainSingle();

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_returns_instances_found_by_discovery_clients()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["discovery:services:0:serviceId"] = "configserver",
            ["discovery:services:0:host"] = "config.example.com",
            ["discovery:services:0:port"] = "8888",
            ["discovery:services:1:serviceId"] = "other-service",
            ["discovery:services:1:host"] = "other.example.com",
            ["discovery:services:1:port"] = "9999"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        IServiceInstance instance = result.Should().ContainSingle().Subject;
        instance.ServiceId.Should().Be("configserver");
        instance.Host.Should().Be("config.example.com");
        instance.Port.Should().Be(8888);

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_returns_nothing_when_Config_Server_is_not_discovered()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["discovery:services:0:serviceId"] = "other-service",
            ["discovery:services:0:host"] = "other.example.com",
            ["discovery:services:0:port"] = "9999"
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();

        await service.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetConfigServerInstances_retries_when_no_instances_are_found()
    {
        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().BuildAsRoot();

        var options = new ConfigServerClientOptions
        {
            Retry =
            {
                Enabled = true,
                MaxAttempts = 2,
                InitialInterval = 10
            }
        };

        var discoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        await service.ProvideRuntimeReplacementsAsync([discoveryClient], TestContext.Current.CancellationToken);

        List<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        discoveryClient.GetInstancesCallCount.Should().Be(3);
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        using ConfigurationRoot configurationRoot =
            new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).BuildAsRoot();

        var discoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);

        await service.ProvideRuntimeReplacementsAsync([discoveryClient], TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().ContainSingle().Which.Should().Be(discoveryClient);
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
        private int _getInstancesCallCount;

        public int GetInstancesCallCount => _getInstancesCallCount;

        public string Description => throw new NotImplementedException();

#pragma warning disable CS0067 // The event is never used
        public event EventHandler<DiscoveryInstancesFetchedEventArgs>? InstancesFetched;
#pragma warning restore CS0067 // The event is never used

        public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _getInstancesCallCount);
            return Task.FromResult<IList<IServiceInstance>>([]);
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
