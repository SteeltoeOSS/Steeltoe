// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// A discovery client for
/// <see href="https://spring.io/guides/gs/service-registration-and-discovery/">
/// Spring Cloud Eureka
/// </see>
/// .
/// </summary>
public sealed class EurekaDiscoveryClient : IDiscoveryClient
{
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly ILogger<EurekaDiscoveryClient> _logger;
    private readonly EurekaClient _eurekaClient;
    private readonly EurekaApplicationInfoManager _appInfoManager;
    private readonly Timer? _heartbeatTimer;
    private readonly Timer? _cacheRefreshTimer;
    private readonly IDisposable? _instanceOptionsChangeToken;
    private readonly IDisposable? _clientOptionsChangeToken;

    private volatile Applications _remoteApps;
    private long _registryFetchCounter;
    private int _shutdown;

    internal bool IsHeartbeatTimerStarted => _heartbeatTimer != null;
    internal bool IsCacheRefreshTimerStarted => _cacheRefreshTimer != null;

    internal DateTime? LastGoodHeartbeatTimeUtc { get; private set; }
    internal DateTime? LastGoodRegistryFetchTimeUtc { get; private set; }
    internal InstanceStatus LastRemoteInstanceStatus { get; private set; } = InstanceStatus.Unknown;

    internal Applications Applications
    {
        get => _remoteApps;
        set => _remoteApps = value;
    }

    internal IHealthCheckHandler? HealthCheckHandler { get; set; }

    public string Description => "A discovery client for Spring Cloud Eureka.";

    public EurekaDiscoveryClient(EurekaApplicationInfoManager appInfoManager, EurekaClient eurekaClient,
        IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(appInfoManager);
        ArgumentGuard.NotNull(eurekaClient);
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        _appInfoManager = appInfoManager;
        _eurekaClient = eurekaClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EurekaDiscoveryClient>();

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        _remoteApps = new Applications
        {
            ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances
        };

        if (!clientOptions.Enabled || clientOptions is { ShouldRegisterWithEureka: false, ShouldFetchRegistry: false })
        {
            return;
        }

        if (clientOptions.ShouldRegisterWithEureka && _appInfoManager.InstanceInfo.LeaseInfo?.RenewalInterval > TimeSpan.Zero)
        {
            try
            {
                RegisterAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.LogInformation(exception, "Initial registration failed.");
            }

            _logger.LogInformation("Starting heartbeat timer.");
            _heartbeatTimer = StartTimer(_appInfoManager.InstanceInfo.LeaseInfo.RenewalInterval.Value, HeartbeatTask);

            _instanceOptionsChangeToken = _appInfoManager.SubscribeToConfigurationChange(options =>
            {
                _appInfoManager.InstanceInfo.UpdateFromConfiguration(options);
                ChangeTimer(_heartbeatTimer, options.LeaseRenewalInterval);
            });

            if (clientOptions.ShouldOnDemandUpdateStatusChange)
            {
                _appInfoManager.StatusChanged += HandleInstanceStatusChanged;
            }
        }

        if (clientOptions.ShouldFetchRegistry)
        {
            try
            {
                FetchRegistryAsync(true, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                _logger.LogInformation(exception, "Initial fetch registry failed.");
            }

            _logger.LogInformation("Starting applications cache refresh timer.");
            _cacheRefreshTimer = StartTimer(clientOptions.RegistryFetchInterval, CacheRefreshTask);

            _clientOptionsChangeToken = _clientOptionsMonitor.OnChange(options => ChangeTimer(_cacheRefreshTimer, options.RegistryFetchInterval));
        }
    }

    internal void SetRegistryFetchCounter(long value)
    {
        Interlocked.Exchange(ref _registryFetchCounter, value);
    }

    internal Application? GetApplication(string appName)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        return Applications.GetRegisteredApplication(appName);
    }

    internal IList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        return secure ? Applications.GetInstancesBySecureVirtualHostName(vipAddress) : Applications.GetInstancesByVirtualHostName(vipAddress);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        int shutdownValue = Interlocked.Exchange(ref _shutdown, 1);

        if (shutdownValue > 0)
        {
            return;
        }

        _appInfoManager.StatusChanged -= HandleInstanceStatusChanged;
        _appInfoManager.InstanceInfo.Status = InstanceStatus.Down;

        _instanceOptionsChangeToken?.Dispose();
        _clientOptionsChangeToken?.Dispose();

        if (_cacheRefreshTimer != null)
        {
            await _cacheRefreshTimer.DisposeAsync();
        }

        if (_heartbeatTimer != null)
        {
            await _heartbeatTimer.DisposeAsync();
        }

        try
        {
            await DeregisterAsync(cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogWarning(exception, "Deregister failed during shutdown.");
        }
    }

    private async void HandleInstanceStatusChanged(object? sender, InstanceStatusChangedEventArgs args)
    {
        try
        {
            InstanceInfo instance = _appInfoManager.InstanceInfo;

            _logger.LogDebug(
                nameof(HandleInstanceStatusChanged) + ": Previous={PreviousStatus}, Current={CurrentStatus}, InstanceId={instanceId}, IsDirty={IsDirty}",
                args.Previous, args.Current, args.InstanceId, instance.IsDirty);

            if (instance.IsDirty)
            {
                _logger.LogDebug("Instance {Application}/{Instance} is marked as dirty, re-registering.", instance.AppName, instance.InstanceId);
                await RegisterAsync(CancellationToken.None);

                _logger.LogInformation($"{nameof(RegisterAsync)} from {nameof(HandleInstanceStatusChanged)} succeeded.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"{nameof(RegisterAsync)} from {nameof(HandleInstanceStatusChanged)} failed.");
        }
    }

    private static Timer StartTimer(TimeSpan interval, Action action)
    {
        var gatedAction = new GatedAction(action);
        return new Timer(_ => gatedAction.Run(), null, interval, interval);
    }

    private static void ChangeTimer(Timer timer, TimeSpan interval)
    {
        timer.Change(interval, interval);
    }

    private async Task FetchRegistryAsync(bool doFullUpdate, CancellationToken cancellationToken)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        bool requireFullFetch = doFullUpdate || !string.IsNullOrWhiteSpace(clientOptions.RegistryRefreshSingleVipAddress) ||
            clientOptions.IsFetchDeltaDisabled || _remoteApps.GetRegisteredApplications().Count == 0;

        Applications? fetched = requireFullFetch ? await FetchFullRegistryAsync(cancellationToken) : await FetchRegistryDeltaAsync(cancellationToken);

        if (fetched != null)
        {
            _remoteApps = fetched;
            _remoteApps.ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances;
            LastGoodRegistryFetchTimeUtc = DateTime.UtcNow;

            UpdateLastRemoteInstanceStatusFromCache();
        }
    }

    internal async Task RegisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        await _eurekaClient.RegisterAsync(instance, cancellationToken);
        _logger.LogDebug("Register {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);

        instance.IsDirty = false;
    }

    internal async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        await _eurekaClient.DeregisterAsync(instance.AppName, instance.InstanceId, cancellationToken);
        _logger.LogDebug("Deregister {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);
    }

    internal async Task RenewAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        await RefreshAppInstanceAsync(cancellationToken);

        if (instance.IsDirty)
        {
            _logger.LogDebug("Instance {Application}/{Instance} is marked as dirty, re-registering.", instance.AppName, instance.InstanceId);
            await RegisterAsync(cancellationToken);
        }

        try
        {
            await _eurekaClient.HeartbeatAsync(instance.AppName, instance.InstanceId, instance.LastDirtyTimeUtc, cancellationToken);
            LastGoodHeartbeatTimeUtc = DateTime.UtcNow;

            _logger.LogDebug("Renew {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogWarning(exception,
                "Eureka heartbeat failed. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");

            await RegisterAsync(cancellationToken);
        }
    }

    public async Task<Applications?> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        long startingCounter = Interlocked.Read(ref _registryFetchCounter);
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        Applications applications = string.IsNullOrWhiteSpace(clientOptions.RegistryRefreshSingleVipAddress)
            ? await _eurekaClient.GetApplicationsAsync(cancellationToken)
            : await _eurekaClient.GetVipAsync(clientOptions.RegistryRefreshSingleVipAddress, cancellationToken);

        if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) != startingCounter)
        {
            _logger.LogWarning("Discarding the results from full registry fetch, due to race condition.");
            return null;
        }

        _logger.LogDebug("Full registry fetch succeeded.");
        return applications;
    }

    internal async Task<Applications?> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
    {
        long startingCounter = Interlocked.Read(ref _registryFetchCounter);
        Applications delta;

        try
        {
            delta = await _eurekaClient.GetDeltaAsync(cancellationToken);
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogDebug(exception, "Failed to fetch registry delta. Trying full fetch.");
            return await FetchFullRegistryAsync(cancellationToken);
        }

        if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) != startingCounter)
        {
            _logger.LogWarning("Discarding the results from registry delta fetch, due to race condition.");
            return null;
        }

        _logger.LogDebug("Registry delta fetched, updating local cache.");
        _remoteApps.UpdateFromDelta(delta);

        string hashCode = _remoteApps.ComputeHashCode();

        if (hashCode != delta.AppsHashCode)
        {
            _logger.LogWarning("Discarding fetched registry delta due to hash codes mismatch (Local={HashLocal}, Remote={HashRemote}). Trying full fetch.",
                hashCode, delta.AppsHashCode);

            return await FetchFullRegistryAsync(cancellationToken);
        }

        _logger.LogDebug("Registry delta fetch succeeded.");
        _remoteApps.AppsHashCode = delta.AppsHashCode;
        return _remoteApps;
    }

    internal async Task RefreshAppInstanceAsync(CancellationToken cancellationToken)
    {
        if (_clientOptionsMonitor.CurrentValue.Health.CheckEnabled && HealthCheckHandler != null)
        {
            try
            {
                InstanceStatus aggregatedStatus = await HealthCheckHandler.GetStatusAsync(cancellationToken);
                _logger.LogDebug("Health check handler returned status {status}.", aggregatedStatus);

                _appInfoManager.InstanceInfo.Status = aggregatedStatus;
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Failed to determine health status.");
            }
        }
    }

    private void UpdateLastRemoteInstanceStatusFromCache()
    {
        InstanceInfo instance = _appInfoManager.InstanceInfo;
        Application? app = GetApplication(instance.AppName);

        if (app != null)
        {
            InstanceInfo? remoteInstance = app.GetInstance(instance.InstanceId);
            LastRemoteInstanceStatus = remoteInstance?.Status ?? InstanceStatus.Unknown;
        }
    }

    private async void HeartbeatTask()
    {
        try
        {
            int shutdownValue = Interlocked.Add(ref _shutdown, 0);

            if (shutdownValue > 0)
            {
                return;
            }

            await RenewAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Periodic Eureka heartbeat failed.");
        }
    }

    private async void CacheRefreshTask()
    {
        try
        {
            int shutdownValue = Interlocked.Add(ref _shutdown, 0);

            if (shutdownValue > 0)
            {
                return;
            }

            await FetchRegistryAsync(false, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Periodic Eureka applications cache refresh failed.");
        }
    }

    /// <inheritdoc />
    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        Applications applications = Applications;

        IList<Application> registered = applications.GetRegisteredApplications();
        IList<string> names = new List<string>();

        foreach (Application app in registered)
        {
            if (app.Instances.Count == 0)
            {
                continue;
            }

#pragma warning disable S4040 // Strings should be normalized to uppercase
            names.Add(app.Name.ToLowerInvariant());
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return Task.FromResult(names);
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        IList<InstanceInfo> nonSecureInstances = GetInstancesByVipAddress(serviceId, false);
        IList<InstanceInfo> secureInstances = GetInstancesByVipAddress(serviceId, true);

        InstanceInfo[] instances = secureInstances.Concat(nonSecureInstances).DistinctBy(instance => instance.InstanceId).ToArray();
        IList<IServiceInstance> serviceInstances = instances.Select(instance => new EurekaServiceInstance(instance)).Cast<IServiceInstance>().ToList();

        _logger.LogDebug($"Returning service instances: {string.Join(',', serviceInstances.Select(instance =>
            $"{instance.ServiceId}={instance.Uri}"))}");

        return Task.FromResult(serviceInstances);
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return new EurekaServiceInstance(_appInfoManager.InstanceInfo);
    }
}
