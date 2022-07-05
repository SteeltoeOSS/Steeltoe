// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Logging;
using Steeltoe.Discovery;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Replace bootstrapped components used by ConfigServerConfigurationProvider with objects provided by Dependency Injection
/// </summary>
public class ConfigServerHostedService : IHostedService
{
    private readonly ConfigServerConfigurationProvider _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDiscoveryClient _discoveryClient;

    public ConfigServerHostedService(IConfigurationRoot configuration, ILoggerFactory loggerFactory, IDiscoveryClient discoveryClient = null)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (configuration.Providers.Count() == 1 && configuration.Providers.First() is PlaceholderResolverProvider resolverProvider)
        {
            _configuration =
                resolverProvider.Providers.First(provider => provider is ConfigServerConfigurationProvider) as
                    ConfigServerConfigurationProvider;
        }
        else
        {
            _configuration = configuration.Providers.First(provider => provider is ConfigServerConfigurationProvider) as ConfigServerConfigurationProvider;
        }

        _loggerFactory = loggerFactory ?? BootstrapLoggerFactory.Instance;
        _discoveryClient = discoveryClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _configuration.ProvideRuntimeReplacementsAsync(_discoveryClient, _loggerFactory);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _configuration.ShutdownAsync();
    }
}
