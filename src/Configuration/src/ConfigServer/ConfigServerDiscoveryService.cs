// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;
using Steeltoe.Discovery.Client;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerDiscoveryService
{
    private readonly IConfiguration _configuration;
    private readonly ConfigServerClientSettings _settings;
    private readonly ILogger _logger;

    private bool _usingInitialDiscoveryClient = true;
    internal IDiscoveryClient DiscoveryClient { get; private set; }

    public ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientSettings settings, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(loggerFactory);

        _configuration = configuration;
        _settings = settings;
        _logger = loggerFactory.CreateLogger<ConfigServerDiscoveryService>();
        DiscoveryClient = SetupDiscoveryClient(loggerFactory);
    }

    // Create a discovery client to be used (hopefully only) during startup
    private IDiscoveryClient SetupDiscoveryClient(ILoggerFactory loggerFactory)
    {
        var tempServices = new ServiceCollection();
        tempServices.AddSingleton(loggerFactory);
        tempServices.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        // force settings to make sure we don't register the app here
        IConfigurationBuilder builder = new ConfigurationBuilder().AddConfiguration(_configuration).AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Eureka:Client:ShouldRegisterWithEureka", "false" },
            { "Consul:Discovery:Register", "false" }
        });

        tempServices.AddSingleton<IConfiguration>(builder.Build());
        tempServices.AddDiscoveryClient(_configuration);

        using ServiceProvider tempServiceProvider = tempServices.BuildServiceProvider();

        var discoveryClient = tempServiceProvider.GetRequiredService<IDiscoveryClient>();
        _logger.LogDebug("Found Discovery Client of type {DiscoveryClientType}", discoveryClient.GetType());

        return discoveryClient;
    }

    internal async Task<IEnumerable<IServiceInstance>> GetConfigServerInstancesAsync(CancellationToken cancellationToken)
    {
        int attempts = 0;
        int backOff = _settings.RetryInitialInterval;
        IList<IServiceInstance> instances;

        do
        {
            try
            {
                _logger.LogDebug("Locating configserver {serviceId} via discovery", _settings.DiscoveryServiceId);
                instances = await DiscoveryClient.GetInstancesAsync(_settings.DiscoveryServiceId, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Exception invoking GetInstances() during config server lookup");

                instances = new List<IServiceInstance>();
            }

            if (!_settings.RetryEnabled || instances.Any())
            {
                break;
            }

            attempts++;

            if (attempts <= _settings.RetryAttempts)
            {
                Thread.CurrentThread.Join(backOff);
                int nextBackOff = (int)(backOff * _settings.RetryMultiplier);
                backOff = Math.Min(nextBackOff, _settings.RetryMaxInterval);
            }
            else
            {
                break;
            }
        }
        while (true);

        return instances;
    }

    internal async Task ProvideRuntimeReplacementsAsync(IDiscoveryClient? discoveryClientFromDI, CancellationToken cancellationToken)
    {
        if (discoveryClientFromDI is not null)
        {
            _logger.LogInformation("Replacing the IDiscoveryClient built at startup with one for runtime");
            await DiscoveryClient.ShutdownAsync(cancellationToken);
            DiscoveryClient = discoveryClientFromDI;
            _usingInitialDiscoveryClient = false;
        }
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_usingInitialDiscoveryClient)
        {
            await DiscoveryClient.ShutdownAsync(cancellationToken);
        }
    }
}
