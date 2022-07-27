// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Logging;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

internal class ConfigServerDiscoveryService
{
    protected internal IConfiguration _configuration;
    protected internal ConfigServerClientSettings _settings;
    protected internal ILoggerFactory _logFactory;
    protected internal ILogger _logger;
    protected internal IDiscoveryClient _discoveryClient;
    private bool _usingInitialDiscoveryClient = true;

    internal ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientSettings settings, ILoggerFactory logFactory = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logFactory = logFactory ?? BootstrapLoggerFactory.Instance;
        _logger = _logFactory.CreateLogger<ConfigServerDiscoveryService>();
        SetupDiscoveryClient();
    }

    // Create a discovery client to be used (hopefully only) during startup
    internal void SetupDiscoveryClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_logFactory);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        // force settings to make sure we don't register the app here
        var cfgBuilder = new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    { "Eureka:Client:ShouldRegisterWithEureka", "false" },
                    { "Consul:Discovery:Register", "false" }
                });

        services.AddSingleton<IConfiguration>(cfgBuilder.Build());
        services.AddDiscoveryClient(_configuration);

        using var startupServiceProvider = services.BuildServiceProvider();
        _discoveryClient = startupServiceProvider.GetRequiredService<IDiscoveryClient>();
        _logger.LogDebug("Found Discovery Client of type {DiscoveryClientType}", _discoveryClient.GetType());
    }

    internal IEnumerable<IServiceInstance> GetConfigServerInstances()
    {
        var attempts = 0;
        var backOff = _settings.RetryInitialInterval;
        IEnumerable<IServiceInstance> instances;
        do
        {
            try
            {
                _logger.LogDebug("Locating configserver {serviceId} via discovery", _settings.DiscoveryServiceId);
                instances = _discoveryClient.GetInstances(_settings.DiscoveryServiceId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception invoking GetInstances() during config server lookup");
                instances = Enumerable.Empty<IServiceInstance>();
            }

            if (!_settings.RetryEnabled || instances.Any())
            {
                break;
            }

            attempts++;
            if (attempts <= _settings.RetryAttempts)
            {
                Thread.CurrentThread.Join(backOff);
                var nextBackoff = (int)(backOff * _settings.RetryMultiplier);
                backOff = Math.Min(nextBackoff, _settings.RetryMaxInterval);
            }
            else
            {
                break;
            }
        }
        while (true);

        return instances;
    }

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient discoveryClientFromDI, ILoggerFactory loggerFactory)
    {
        if (discoveryClientFromDI is not null)
        {
            _logger.LogInformation("Replacing the IDiscoveryClient built at startup with one for runtime");
            await _discoveryClient.ShutdownAsync().ConfigureAwait(false);
            _discoveryClient = discoveryClientFromDI;
            _usingInitialDiscoveryClient = false;
        }
    }

    internal async Task ShutdownAsync()
    {
        if (_usingInitialDiscoveryClient)
        {
            await _discoveryClient.ShutdownAsync();
        }
    }
}