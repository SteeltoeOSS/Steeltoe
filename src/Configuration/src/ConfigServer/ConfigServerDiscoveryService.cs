// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

namespace Steeltoe.Configuration.ConfigServer;

internal sealed class ConfigServerDiscoveryService
{
    private static readonly AssemblyLoader AssemblyLoader = new();
    private readonly IConfiguration _configuration;
    private readonly ConfigServerClientOptions _options;
    private readonly ILogger _logger;
    private ServiceProvider? _temporaryServiceProviderForDiscoveryClients;

    internal ICollection<IDiscoveryClient> DiscoveryClients { get; private set; }

    public ConfigServerDiscoveryService(IConfiguration configuration, ConfigServerClientOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(loggerFactory);

        _configuration = configuration;
        _options = options;
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
            { "Eureka:Client:ShouldFetchRegistry", "true" },
            { "Consul:Discovery:Register", "false" }
        }).Build();

        tempServices.AddSingleton<IConfiguration>(tempConfiguration);

        if (AssemblyLoader.IsAssemblyLoaded("Steeltoe.Discovery.Configuration"))
        {
            WireConfigurationDiscoveryClient(tempServices);
        }

        if (AssemblyLoader.IsAssemblyLoaded("Steeltoe.Discovery.Consul"))
        {
            WireConsulDiscoveryClient(tempServices);
        }

        if (AssemblyLoader.IsAssemblyLoaded("Steeltoe.Discovery.Eureka"))
        {
            WireEurekaDiscoveryClient(tempServices);
        }

        return GetDiscoveryClientsFromServiceCollection(tempServices);
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

    private ICollection<IDiscoveryClient> GetDiscoveryClientsFromServiceCollection(ServiceCollection services)
    {
        _temporaryServiceProviderForDiscoveryClients = services.BuildServiceProvider();

        IDiscoveryClient[] discoveryClients = _temporaryServiceProviderForDiscoveryClients.GetServices<IDiscoveryClient>().ToArray();

        foreach (IDiscoveryClient discoveryClient in discoveryClients)
        {
            _logger.LogDebug("Found discovery client of type {DiscoveryClientType}", discoveryClient.GetType());
        }

        return discoveryClients;
    }

    internal async Task<IEnumerable<IServiceInstance>> GetConfigServerInstancesAsync(CancellationToken cancellationToken)
    {
        int attempts = 0;
        int backOff = _options.Retry.InitialInterval;
        List<IServiceInstance> instances = [];

        do
        {
            _logger.LogDebug("Locating ConfigServer {ServiceId} via discovery", _options.Discovery.ServiceId);

            if (_options.Discovery.ServiceId != null)
            {
                foreach (IDiscoveryClient discoveryClient in DiscoveryClients)
                {
                    try
                    {
                        IList<IServiceInstance> serviceInstances = await discoveryClient.GetInstancesAsync(_options.Discovery.ServiceId, cancellationToken);
                        instances.AddRange(serviceInstances);
                    }
                    catch (Exception exception) when (!exception.IsCancellation())
                    {
                        _logger.LogError(exception, "Failed to get instances during ConfigServer lookup from {DiscoveryClient}.", discoveryClient.GetType());
                    }
                }
            }

            if (!_options.Retry.Enabled || instances.Any())
            {
                break;
            }

            attempts++;

            if (attempts <= _options.Retry.MaxAttempts)
            {
                Thread.CurrentThread.Join(backOff);
                int nextBackOff = (int)(backOff * _options.Retry.Multiplier);
                backOff = Math.Min(nextBackOff, _options.Retry.MaxInterval);
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
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_temporaryServiceProviderForDiscoveryClients != null)
        {
            foreach (IDiscoveryClient discoveryClient in DiscoveryClients)
            {
                await discoveryClient.ShutdownAsync(cancellationToken);
            }

            await _temporaryServiceProviderForDiscoveryClients.DisposeAsync();
            _temporaryServiceProviderForDiscoveryClients = null;
        }
    }
}
