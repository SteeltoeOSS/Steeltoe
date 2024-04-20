// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Reflection;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerDiscoveryService
{
    private readonly IConfiguration _configuration;
    private readonly ConfigServerClientSettings _settings;
    private readonly ILogger _logger;

    private bool _isUsingTemporaryDiscoveryClients = true;
    internal ICollection<IDiscoveryClient> DiscoveryClients { get; private set; }

    public ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientSettings settings, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(loggerFactory);

        _configuration = configuration;
        _settings = settings;
        _logger = loggerFactory.CreateLogger<ConfigServerDiscoveryService>();
        DiscoveryClients = SetupDiscoveryClients(loggerFactory);
    }

    // Create discovery clients to be used (hopefully only) during startup
    private ICollection<IDiscoveryClient> SetupDiscoveryClients(ILoggerFactory loggerFactory)
    {
        var tempServices = new ServiceCollection();
        tempServices.AddSingleton(loggerFactory);
        tempServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // force settings to make sure we don't register the app here
        IConfigurationRoot tempConfiguration = new ConfigurationBuilder().AddConfiguration(_configuration).AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Eureka:Client:ShouldRegisterWithEureka", "false" },
            { "Consul:Discovery:Register", "false" }
        }).Build();

        tempServices.AddSingleton<IConfiguration>(tempConfiguration);

        if (ReflectionHelpers.IsAssemblyLoaded("Steeltoe.Discovery.Configuration"))
        {
            WireConfigurationDiscoveryClient(tempServices);
        }

        if (ReflectionHelpers.IsAssemblyLoaded("Steeltoe.Discovery.Consul"))
        {
            WireConsulDiscoveryClient(tempServices);
        }

        if (ReflectionHelpers.IsAssemblyLoaded("Steeltoe.Discovery.Eureka"))
        {
            WireEurekaDiscoveryClient(tempServices);
        }

        return GetDiscoveryClientsFromServiceCollectionAsync(tempServices).GetAwaiter().GetResult();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConfigurationDiscoveryClient(IServiceCollection tempServices)
    {
        tempServices.AddConfigurationDiscoveryClient();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireConsulDiscoveryClient(IServiceCollection tempServices)
    {
        tempServices.AddConsulDiscoveryClient();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WireEurekaDiscoveryClient(IServiceCollection tempServices)
    {
        tempServices.AddEurekaDiscoveryClient();
    }

    private async Task<ICollection<IDiscoveryClient>> GetDiscoveryClientsFromServiceCollectionAsync(ServiceCollection services)
    {
        await using ServiceProvider tempServiceProvider = services.BuildServiceProvider();

        IDiscoveryClient[] discoveryClients = tempServiceProvider.GetRequiredService<IEnumerable<IDiscoveryClient>>().ToArray();

        foreach (IDiscoveryClient discoveryClient in discoveryClients)
        {
            _logger.LogDebug("Found discovery client of type {DiscoveryClientType}", discoveryClient.GetType());
        }

        return discoveryClients;
    }

    internal async Task<IEnumerable<IServiceInstance>> GetConfigServerInstancesAsync(CancellationToken cancellationToken)
    {
        int attempts = 0;
        int backOff = _settings.RetryInitialInterval;
        List<IServiceInstance> instances = [];

        do
        {
            try
            {
                _logger.LogDebug("Locating configserver {serviceId} via discovery", _settings.DiscoveryServiceId);

                if (_settings.DiscoveryServiceId != null)
                {
                    foreach (IDiscoveryClient discoveryClient in DiscoveryClients)
                    {
                        IList<IServiceInstance> serviceInstances = await discoveryClient.GetInstancesAsync(_settings.DiscoveryServiceId, cancellationToken);
                        instances.AddRange(serviceInstances);
                    }
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Exception invoking GetInstances() during config server lookup");
                instances = [];
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

    internal async Task ProvideRuntimeReplacementsAsync(ICollection<IDiscoveryClient> discoveryClientsFromServiceProvider, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(discoveryClientsFromServiceProvider);

        _logger.LogInformation("Replacing the IDiscoveryClient(s) built at startup with the ones for runtime");

        await ShutdownAsync(cancellationToken);

        DiscoveryClients = discoveryClientsFromServiceProvider;
        _isUsingTemporaryDiscoveryClients = false;
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_isUsingTemporaryDiscoveryClients)
        {
            foreach (IDiscoveryClient discoveryClient in DiscoveryClients)
            {
                await discoveryClient.ShutdownAsync(cancellationToken);
            }
        }
    }
}
