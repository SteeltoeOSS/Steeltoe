// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Replace bootstrapped components used by <see cref="ConfigServerConfigurationProvider" /> with objects provided by Dependency Injection.
/// </summary>
internal sealed class ConfigServerHostedService : IHostedService
{
    private readonly ConfigServerConfigurationProvider _configurationProvider;
    private readonly IDiscoveryClient[] _discoveryClients;

    public ConfigServerHostedService(IConfigurationRoot configuration)
        : this(configuration, [])
    {
    }

    public ConfigServerHostedService(IConfigurationRoot configuration, IEnumerable<IDiscoveryClient> discoveryClients)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(discoveryClients);

        IDiscoveryClient[] discoveryClientArray = discoveryClients.ToArray();
        ArgumentGuard.ElementsNotNull(discoveryClientArray);

        _configurationProvider = configuration.FindConfigurationProvider<ConfigServerConfigurationProvider>() ??
            throw new ArgumentException("ConfigServerConfigurationProvider was not found in configuration.", nameof(configuration));

        _discoveryClients = discoveryClientArray;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _configurationProvider.ProvideRuntimeReplacementsAsync(_discoveryClients, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _configurationProvider.ShutdownAsync(cancellationToken);
    }
}
