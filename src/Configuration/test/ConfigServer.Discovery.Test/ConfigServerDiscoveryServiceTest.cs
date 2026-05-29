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
        IConfiguration configuration = new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).Build();

        var service = new ConfigServerDiscoveryService(configuration, NullLoggerFactory.Instance);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().HaveCount(3);
        service.DiscoveryClients.OfType<ConfigurationDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<ConsulDiscoveryClient>().Should().ContainSingle();
        service.DiscoveryClients.OfType<EurekaDiscoveryClient>().Should().ContainSingle();
    }

    [Fact]
    public async Task InvokeGetInstances_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["eureka:client:eurekaServer:retryCount"] = "0"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = builder.Build();
        var options = new ConfigServerClientOptions();

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeGetInstances_RetryEnabled_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["eureka:client:eurekaServer:retryCount"] = "1"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var options = new ConfigServerClientOptions
        {
            Retry =
            {
                Enabled = true,
                MaxAttempts = 1
            },
            Timeout = 10
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfigServerInstances_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery))
        {
            ["eureka:client:eurekaServer:retryCount"] = "0"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new ConfigServerClientOptions
        {
            Retry =
            {
                Enabled = false
            },
            Timeout = 10
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(options, TestContext.Current.CancellationToken);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer | FastTestConfigurations.Discovery).Build();

        var testDiscoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, NullLoggerFactory.Instance);

        await service.ProvideRuntimeReplacementsAsync([testDiscoveryClient], TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().ContainSingle().Which.Should().Be(testDiscoveryClient);
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
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
            throw new NotImplementedException();
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
