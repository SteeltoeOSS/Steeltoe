// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Configuration;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Eureka;

// Justification: SemaphoreSlim only allocates a wait handle when AvailableWaitHandle is accessed, which is never done here.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace Steeltoe.Configuration.ConfigServer;

internal sealed partial class ConfigServerDiscoveryService
{
    private static readonly AssemblyLoader AssemblyLoader = new();
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private Action<IServiceCollection>? _configureTemporaryServices;
    private ServiceProvider? _temporaryServiceProviderForDiscoveryClients;
    private volatile ICollection<IDiscoveryClient>? _discoveryClients;

    internal ICollection<IDiscoveryClient>? DiscoveryClients
    {
        get => _discoveryClients;
        private set => _discoveryClients = value;
    }

    /// <summary>
    /// Gets a value indicating whether the discovery clients used to locate Config Server are still active. They are active until they get replaced (when
    /// the application starts) or shut down.
    /// </summary>
    internal bool HasTemporaryDiscoveryClients => _temporaryServiceProviderForDiscoveryClients != null;

    public ConfigServerDiscoveryService(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _configuration = configuration;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ConfigServerDiscoveryService>();
    }

    /// <summary>
    /// Allows tests to add services (such as fake discovery clients) to the temporary container that the discovery clients are created in. Must be called
    /// before the first lookup.
    /// </summary>
    /// <param name="configureServices">
    /// Adds services to the temporary container.
    /// </param>
    internal void ConfigureTemporaryServicesForTest(Action<IServiceCollection> configureServices)
    {
        ArgumentNullException.ThrowIfNull(configureServices);

        _configureTemporaryServices = configureServices;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_discoveryClients == null)
        {
            await _initLock.WaitAsync(cancellationToken);

            try
            {
                if (_discoveryClients == null)
                {
                    await SetupDiscoveryClientsAsync();
                }
            }
            finally
            {
                _initLock.Release();
            }
        }
    }

    private async Task SetupDiscoveryClientsAsync()
    {
        var tempServices = new ServiceCollection();
        tempServices.AddSingleton(_loggerFactory);
        tempServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // force settings to make sure we don't register the app here
        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldRegisterWithEureka"] = "false",
            ["Eureka:Client:ShouldFetchRegistry"] = "true",
            ["Consul:Discovery:Register"] = "false"
        };

        IConfigurationRoot tempConfigurationRoot = new ConfigurationBuilder().AddConfiguration(_configuration).AddInMemoryCollection(appSettings).Build();
        tempServices.AddSingleton<IConfiguration>(_ => tempConfigurationRoot);

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

        _configureTemporaryServices?.Invoke(tempServices);

        _discoveryClients = await GetDiscoveryClientsFromServiceCollectionAsync(tempServices);
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

    private async Task<IDiscoveryClient[]> GetDiscoveryClientsFromServiceCollectionAsync(ServiceCollection services)
    {
        ServiceProvider temporaryServiceProvider = services.BuildServiceProvider();

        try
        {
            IDiscoveryClient[] discoveryClients = temporaryServiceProvider.GetServices<IDiscoveryClient>().ToArray();

            foreach (IDiscoveryClient discoveryClient in discoveryClients)
            {
                LogDiscoveryClientFound(discoveryClient.GetType());
            }

            _temporaryServiceProviderForDiscoveryClients = temporaryServiceProvider;
            return discoveryClients;
        }
        catch
        {
            try
            {
                await temporaryServiceProvider.DisposeAsync();
            }
            catch
            {
                // Intentionally left empty: the original exception is more useful.
            }

            throw;
        }
    }

    internal async Task<List<IServiceInstance>> GetConfigServerInstancesAsync(ConfigServerClientOptions optionsSnapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(optionsSnapshot);

        await EnsureInitializedAsync(cancellationToken);

        int attempts = 0;
        int backOff = optionsSnapshot.Retry.InitialInterval;
        List<IServiceInstance> instances = [];

        do
        {
            LogLocatingConfigServer(optionsSnapshot.Discovery.ServiceId);

            if (optionsSnapshot.Discovery.ServiceId != null)
            {
                await AddServiceInstancesFromDiscoveryClientsAsync(optionsSnapshot.Discovery.ServiceId, instances, cancellationToken);
            }

            if (!optionsSnapshot.Retry.Enabled || instances.Count > 0)
            {
                break;
            }

            attempts++;

            if (attempts <= optionsSnapshot.Retry.MaxAttempts)
            {
                await Task.Delay(backOff, cancellationToken);
                int nextBackOff = (int)(backOff * optionsSnapshot.Retry.Multiplier);
                backOff = Math.Min(nextBackOff, optionsSnapshot.Retry.MaxInterval);
            }
            else
            {
                break;
            }
        }
        while (true);

        return instances;
    }

    private async Task AddServiceInstancesFromDiscoveryClientsAsync(string serviceId, List<IServiceInstance> instances, CancellationToken cancellationToken)
    {
        foreach (IDiscoveryClient discoveryClient in _discoveryClients ?? [])
        {
            try
            {
                IList<IServiceInstance> serviceInstances = await discoveryClient.GetInstancesAsync(serviceId, cancellationToken);
                instances.AddRange(serviceInstances);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                LogFailedToGetInstances(exception, discoveryClient.GetType());
            }
        }
    }

    internal async Task ProvideRuntimeReplacementsAsync(ICollection<IDiscoveryClient> discoveryClientsFromServiceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(discoveryClientsFromServiceProvider);

        LogReplacingDiscoveryClients();

        try
        {
            await ShutdownAsync(cancellationToken);
        }
        finally
        {
            // Even when shutting down the temporary discovery clients failed, the replacements must become active.
            DiscoveryClients = discoveryClientsFromServiceProvider;
        }
    }

    internal async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        ServiceProvider? temporaryServiceProvider = _temporaryServiceProviderForDiscoveryClients;

        if (temporaryServiceProvider == null)
        {
            return;
        }

        List<Exception> exceptions = [];

        foreach (IDiscoveryClient discoveryClient in _discoveryClients ?? [])
        {
            try
            {
                await discoveryClient.ShutdownAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        try
        {
            await temporaryServiceProvider.DisposeAsync();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
        finally
        {
            _temporaryServiceProviderForDiscoveryClients = null;
        }

        if (exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found discovery client of type {DiscoveryClientType}.")]
    private partial void LogDiscoveryClientFound(Type discoveryClientType);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Locating ConfigServer {ServiceId} via discovery.")]
    private partial void LogLocatingConfigServer(string? serviceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get instances during ConfigServer lookup from {DiscoveryClient}.")]
    private partial void LogFailedToGetInstances(Exception exception, Type discoveryClient);

    [LoggerMessage(Level = LogLevel.Information, Message = "Replacing the IDiscoveryClient(s) built at startup with the ones for runtime.")]
    private partial void LogReplacingDiscoveryClients();
}
