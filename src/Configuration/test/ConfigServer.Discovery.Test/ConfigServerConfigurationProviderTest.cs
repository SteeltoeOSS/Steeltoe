// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Discovery.Test;

public sealed class ConfigServerConfigurationProviderTest
{
    [Fact]
    public void Disposing_provider_shuts_down_temporary_discovery_clients()
    {
        using var handler = new DelegateToMockHttpClientHandler();
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        var builder = new ConfigurationBuilder();
        builder.Add(FastTestConfigurations.Discovery);

        var options = new ConfigServerClientOptions
        {
            Discovery =
            {
                Enabled = true
            }
        };

        // ReSharper disable once AccessToDisposedClosure
        builder.AddConfigServer(options, null, () => handler, NullLoggerFactory.Instance);

        ConfigServerConfigurationProvider provider;

        using (ConfigurationRoot configurationRoot = builder.BuildAsRoot())
        {
            provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

            // These clients are used to locate Config Server while loading configuration. In a host, they get replaced when the application starts.
            provider.HasTemporaryDiscoveryClients.Should().BeTrue();
        }

        provider.HasTemporaryDiscoveryClients.Should().BeFalse();
    }

    [Fact]
    public async Task Replacing_discovery_clients_shuts_down_temporary_discovery_clients()
    {
        using var handler = new DelegateToMockHttpClientHandler();
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        var builder = new ConfigurationBuilder();
        builder.Add(FastTestConfigurations.Discovery);

        var options = new ConfigServerClientOptions
        {
            Discovery =
            {
                Enabled = true
            }
        };

        // ReSharper disable once AccessToDisposedClosure
        builder.AddConfigServer(options, null, () => handler, NullLoggerFactory.Instance);

        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();
        ConfigServerConfigurationProvider provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        // This is what the hosted service does at startup. Disposing the configuration afterward (via using) has nothing left to shut down.
        await provider.ProvideRuntimeReplacementsAsync([], TestContext.Current.CancellationToken);

        provider.HasTemporaryDiscoveryClients.Should().BeFalse();
    }
}
