// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Discovery;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Replace bootstrapped components used by <see cref="ConfigServerConfigurationProvider" /> with objects provided by Dependency Injection.
/// </summary>
internal sealed class ConfigServerHostedService : IHostedService
{
    private readonly ConfigServerConfigurationProvider _configuration;
    private readonly IDiscoveryClient _discoveryClient;

    public ConfigServerHostedService(IConfigurationRoot configuration, ILoggerFactory loggerFactory)
        : this(configuration, loggerFactory, null)
    {
    }

    public ConfigServerHostedService(IConfigurationRoot configuration, ILoggerFactory loggerFactory, IDiscoveryClient discoveryClient)
    {
        ArgumentGuard.NotNull(configuration);

        if (configuration.Providers.Count() == 1 && configuration.Providers.First() is PlaceholderResolverProvider resolverProvider)
        {
            _configuration = resolverProvider.Providers.OfType<ConfigServerConfigurationProvider>().First();
        }
        else
        {
            _configuration = configuration.Providers.OfType<ConfigServerConfigurationProvider>().First();
        }

        _ = loggerFactory;
        _discoveryClient = discoveryClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _configuration.ProvideRuntimeReplacementsAsync(_discoveryClient, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _configuration.ShutdownAsync(cancellationToken);
    }
}
