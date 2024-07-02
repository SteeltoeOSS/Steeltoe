// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    public void ConfigServerDiscoveryService_FindsDiscoveryClients()
    {
        var appSettings = new Dictionary<string, string?>(TestHelpers.FastTestsConfiguration);

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var clientSettings = new ConfigServerClientSettings();

        var service = new ConfigServerDiscoveryService(configuration, clientSettings, NullLoggerFactory.Instance);

        Assert.Equal(3, service.DiscoveryClients.Count);
        Assert.Contains(service.DiscoveryClients, discoveryClient => discoveryClient is ConfigurationDiscoveryClient);
        Assert.Contains(service.DiscoveryClients, discoveryClient => discoveryClient is ConsulDiscoveryClient);
        Assert.Contains(service.DiscoveryClients, discoveryClient => discoveryClient is EurekaDiscoveryClient);
    }

    [Fact]
    public async Task InvokeGetInstances_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestHelpers.FastTestsConfiguration)
        {
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = builder.Build();
        var settings = new ConfigServerClientSettings();

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task InvokeGetInstances_RetryEnabled_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestHelpers.FastTestsConfiguration)
        {
            { "eureka:client:eurekaServer:retryCount", "1" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var settings = new ConfigServerClientSettings
        {
            Retry =
            {
                Enabled = true,
                Attempts = 1
            },
            Timeout = 10
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetConfigServerInstances_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>(TestHelpers.FastTestsConfiguration)
        {
            { "eureka:client:eurekaServer:retryCount", "0" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = builder.Build();

        var settings = new ConfigServerClientSettings
        {
            Retry =
            {
                Enabled = false
            },
            Timeout = 10
        };

        var service = new ConfigServerDiscoveryService(configurationRoot, settings, NullLoggerFactory.Instance);
        IEnumerable<IServiceInstance> result = await service.GetConfigServerInstancesAsync(CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task RuntimeReplacementsCanBeProvided()
    {
        ImmutableDictionary<string, string?> appSettings = TestHelpers.FastTestsConfiguration;
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var testDiscoveryClient = new TestDiscoveryClient();
        var service = new ConfigServerDiscoveryService(configurationRoot, new ConfigServerClientSettings(), NullLoggerFactory.Instance);

        await service.ProvideRuntimeReplacementsAsync([testDiscoveryClient], CancellationToken.None);

        service.DiscoveryClients.Should().HaveCount(1);
        service.DiscoveryClients.First().Should().Be(testDiscoveryClient);
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
        public string Description => throw new NotImplementedException();

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
