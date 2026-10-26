// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Discovery.Test;

public sealed class ConfigServerHostedServiceTest
{
    [Fact]
    public async Task Starting_replaces_temporary_discovery_clients()
    {
        using DelegateToMockHttpClientHandler handler = new DelegateToMockHttpClientHandler().RespondToAnyRequest(HttpStatusCode.NotFound);

        // ReSharper disable once AccessToDisposedClosure
        using ConfigurationRoot configurationRoot = BuildConfigurationWithDiscovery(handler);
        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        // These clients are used to locate Config Server while loading configuration. The hosted service replaces them when the application starts.
        provider.HasTemporaryDiscoveryClients.Should().BeTrue();

        var service = new ConfigServerHostedService(configurationRoot, []);
        await service.StartAsync(TestContext.Current.CancellationToken);

        provider.HasTemporaryDiscoveryClients.Should().BeFalse();

        await service.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Stopping_shuts_down_temporary_discovery_clients()
    {
        using DelegateToMockHttpClientHandler handler = new DelegateToMockHttpClientHandler().RespondToAnyRequest(HttpStatusCode.NotFound);

        // ReSharper disable once AccessToDisposedClosure
        using ConfigurationRoot configurationRoot = BuildConfigurationWithDiscovery(handler);
        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        provider.HasTemporaryDiscoveryClients.Should().BeTrue();

        // When the host stops without ever having started, the temporary clients are still active and must be cleaned up.
        var service = new ConfigServerHostedService(configurationRoot, []);
        await service.StopAsync(TestContext.Current.CancellationToken);

        provider.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    private static ConfigurationRoot BuildConfigurationWithDiscovery(DelegateToMockHttpClientHandler handler)
    {
        var builder = new ConfigurationBuilder();
        builder.Add(FastTestConfigurations.Discovery);

        var options = new ConfigServerClientOptions
        {
            Discovery =
            {
                Enabled = true
            }
        };

        builder.AddConfigServer(options, null, () => handler, NullLoggerFactory.Instance);
        return builder.BuildAsRoot();
    }
}
