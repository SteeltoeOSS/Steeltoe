// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
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
    private readonly EurekaApplicationInfoManager _appInfoManager;
    private readonly EurekaClient _eurekaClient;
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly HealthCheckHandlerProvider _healthCheckHandlerProvider;
    private readonly IDisposable? _clientOptionsChangeToken;
    private readonly ILogger<EurekaDiscoveryClient> _logger;
    private readonly Timer? _heartbeatTimer;
    private readonly Timer? _cacheRefreshTimer;
    private readonly SemaphoreSlim _registerUnregisterAsyncLock = new(1);
    private readonly SemaphoreSlim _registryFetchAsyncLock = new(1);
    private volatile bool _hasFirstHeartbeatCompleted;

    private volatile ApplicationInfoCollection _remoteApps;
    private volatile NullableValueWrapper<DateTime> _lastGoodHeartbeatTimeUtc = new(null);
    private volatile NullableValueWrapper<DateTime> _lastGoodRegistryFetchTimeUtc = new(null);
    private volatile NullableValueWrapper<InstanceStatus> _lastRemoteInstanceStatus = new(InstanceStatus.Unknown);

    private int _isShutdown;

    private bool IsAlive => Interlocked.Add(ref _isShutdown, 0) == 0;

    private IHealthCheckHandler HealthCheckHandler => _healthCheckHandlerProvider.GetHandler();

    internal bool IsHeartbeatTimerStarted => _heartbeatTimer != null;
    internal bool IsCacheRefreshTimerStarted => _cacheRefreshTimer != null;

    internal DateTime? LastGoodHeartbeatTimeUtc => _lastGoodHeartbeatTimeUtc.Value;
    internal DateTime? LastGoodRegistryFetchTimeUtc => _lastGoodRegistryFetchTimeUtc.Value;
    internal InstanceStatus LastRemoteInstanceStatus => _lastRemoteInstanceStatus.Value ?? InstanceStatus.Unknown;

    internal ApplicationInfoCollection Applications
    {
        get => _remoteApps;
        set => _remoteApps = value;
    }

    public string Description => "A discovery client for Spring Cloud Eureka.";

    /// <summary>
    /// Occurs after applications have been fetched from Eureka.
    /// </summary>
    public event EventHandler<ApplicationsFetchedEventArgs>? ApplicationsFetched;

    public EurekaDiscoveryClient(EurekaApplicationInfoManager appInfoManager, EurekaClient eurekaClient,
        IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, HealthCheckHandlerProvider healthCheckHandlerProvider, ILogger<EurekaDiscoveryClient> logger)
    {
        ArgumentNullException.ThrowIfNull(appInfoManager);
        ArgumentNullException.ThrowIfNull(eurekaClient);
        ArgumentNullException.ThrowIfNull(clientOptionsMonitor);
        ArgumentNullException.ThrowIfNull(healthCheckHandlerProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _appInfoManager = appInfoManager;
        _eurekaClient = eurekaClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _healthCheckHandlerProvider = healthCheckHandlerProvider;
        _logger = logger;

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        _remoteApps = new ApplicationInfoCollection
        {
            ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances
        };

        if (!clientOptions.Enabled)
        {
            return;
        }

        if (clientOptions.ShouldRegisterWithEureka)
        {
            TimeSpan? leaseRenewalInterval = _appInfoManager.Instance.LeaseInfo?.RenewalInterval;

            if (leaseRenewalInterval > TimeSpan.Zero)
            {
                try
                {
                    // Only register when periodically refreshing. Just once at startup doesn't make sense.
                    RegisterAsync(false, CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    _logger.LogInformation(exception, "Initial registration failed.");
                }

                _logger.LogInformation("Starting heartbeat timer.");
                _heartbeatTimer = StartTimer(leaseRenewalInterval.Value, HeartbeatAsyncTask);

                _appInfoManager.InstanceChanged += AppInfoManagerOnInstanceChanged;
            }
        }

        if (clientOptions.ShouldFetchRegistry && clientOptions.RegistryFetchInterval > TimeSpan.Zero)
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
            _cacheRefreshTimer = StartTimer(clientOptions.RegistryFetchInterval, CacheRefreshAsyncTask);

            _clientOptionsChangeToken = _clientOptionsMonitor.OnChange(options =>
            {
                // If timer started initially, we'll respond to interval change. Turning the timer on/off at any time is not supported.
                ChangeTimer(_cacheRefreshTimer, options.RegistryFetchInterval);
            });
        }
    }

    internal ApplicationInfo? GetApplication(string appName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);

        return Applications.GetRegisteredApplication(appName);
    }

    internal IReadOnlyList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vipAddress);

        return secure ? Applications.GetInstancesBySecureVipAddress(vipAddress) : Applications.GetInstancesByVipAddress(vipAddress);
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        int shutdownValue = Interlocked.Exchange(ref _isShutdown, 1);

        if (shutdownValue > 0)
        {
            return;
        }

        _clientOptionsChangeToken?.Dispose();
        _appInfoManager.InstanceChanged -= AppInfoManagerOnInstanceChanged;

        if (_cacheRefreshTimer != null)
        {
            await _cacheRefreshTimer.DisposeAsync();
        }

        if (_heartbeatTimer != null)
        {
            await _heartbeatTimer.DisposeAsync();
        }

        _appInfoManager.UpdateInstance(InstanceStatus.Down, null, null);

        try
        {
            if (!ReferenceEquals(_appInfoManager.Instance, InstanceInfo.Disabled))
            {
                await DeregisterAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogWarning(exception, "Deregister failed during shutdown.");
        }

        _appInfoManager.Dispose();
        _registerUnregisterAsyncLock.Dispose();
        _registryFetchAsyncLock.Dispose();
    }

    private async void AppInfoManagerOnInstanceChanged(object? sender, InstanceChangedEventArgs args)
    {
        if (!IsAlive)
        {
            return;
        }

        try
        {
            _logger.LogDebug("Instance changed event handler: New={NewInstance}, Previous={PreviousInstance}", args.NewInstance, args.PreviousInstance);

            if (args.NewInstance.LeaseInfo?.RenewalInterval != args.PreviousInstance.LeaseInfo?.RenewalInterval)
            {
                // If timer started initially, we'll respond to interval change. Turning the timer on/off at any time is not supported.
                ChangeTimer(_heartbeatTimer, args.NewInstance.LeaseInfo?.RenewalInterval);
            }

            await RegisterAsync(true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            if (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Failed to handle {EventName} event.", nameof(EurekaApplicationInfoManager.InstanceChanged));
            }
        }
    }

    private static Timer StartTimer(TimeSpan interval, Action action)
    {
        var gatedAction = new GatedAction(action);
        return new Timer(_ => gatedAction.Run(), null, interval, interval);
    }

    private static void ChangeTimer(Timer? timer, TimeSpan? interval)
    {
        if (timer != null && interval > TimeSpan.Zero)
        {
            timer.Change(TimeSpan.Zero, interval.Value);
        }
    }

    internal async Task RegisterAsync(bool requireDirtyInstance, CancellationToken cancellationToken)
    {
        await _registerUnregisterAsyncLock.WaitAsync(cancellationToken);

        try
        {
            InstanceInfo snapshot = _appInfoManager.Instance;

            if (!requireDirtyInstance || snapshot.IsDirty)
            {
                _logger.LogDebug("Registering {Application}/{Instance}.", snapshot.AppName, snapshot.InstanceId);
                await _eurekaClient.RegisterAsync(snapshot, cancellationToken);
                _logger.LogDebug("Register {Application}/{Instance} succeeded.", snapshot.AppName, snapshot.InstanceId);

                snapshot.IsDirty = false;
            }
        }
        finally
        {
            _registerUnregisterAsyncLock.Release();
        }
    }

    internal async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        await _registerUnregisterAsyncLock.WaitAsync(cancellationToken);

        try
        {
            InstanceInfo snapshot = _appInfoManager.Instance;

            _logger.LogDebug("Deregistering {Application}/{Instance}.", snapshot.AppName, snapshot.InstanceId);
            await _eurekaClient.DeregisterAsync(snapshot.AppName, snapshot.InstanceId, cancellationToken);
            _logger.LogDebug("Deregister {Application}/{Instance} succeeded.", snapshot.AppName, snapshot.InstanceId);
        }
        finally
        {
            _registerUnregisterAsyncLock.Release();
        }
    }

    internal async Task RenewAsync(CancellationToken cancellationToken)
    {
        await RunHealthChecksAsync(cancellationToken);
        await RegisterAsync(true, cancellationToken);

        try
        {
            InstanceInfo snapshot = _appInfoManager.Instance;

            _logger.LogDebug("Sending heartbeat for {Application}/{Instance}.", snapshot.AppName, snapshot.InstanceId);
            await _eurekaClient.HeartbeatAsync(snapshot.AppName, snapshot.InstanceId, snapshot.LastDirtyTimeUtc, cancellationToken);
            _logger.LogDebug("Heartbeat for {Application}/{Instance} succeeded.", snapshot.AppName, snapshot.InstanceId);

            _lastGoodHeartbeatTimeUtc = new NullableValueWrapper<DateTime>(DateTime.UtcNow);
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogWarning(exception,
                "Eureka heartbeat failed. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");

            await RegisterAsync(false, cancellationToken);
        }
        finally
        {
            _hasFirstHeartbeatCompleted = true;
        }
    }

    internal async Task FetchRegistryAsync(bool doFullUpdate, CancellationToken cancellationToken)
    {
        if (!IsAlive)
        {
            return;
        }

        await _registryFetchAsyncLock.WaitAsync(cancellationToken);

        ApplicationsFetchedEventArgs eventArgs;

        try
        {
            EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

            bool requireFullFetch = doFullUpdate || !string.IsNullOrWhiteSpace(clientOptions.RegistryRefreshSingleVipAddress) ||
                clientOptions.IsFetchDeltaDisabled || _remoteApps.RegisteredApplications.Count == 0;

            ApplicationInfoCollection fetched =
                requireFullFetch ? await FetchFullRegistryAsync(cancellationToken) : await FetchRegistryDeltaAsync(cancellationToken);

            _remoteApps = fetched;
            _remoteApps.ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances;
            _lastGoodRegistryFetchTimeUtc = new NullableValueWrapper<DateTime>(DateTime.UtcNow);

            UpdateLastRemoteInstanceStatusFromCache();

            eventArgs = new ApplicationsFetchedEventArgs(_remoteApps);
        }
        finally
        {
            _registryFetchAsyncLock.Release();
        }

        OnApplicationsFetched(eventArgs);
    }

    private void OnApplicationsFetched(ApplicationsFetchedEventArgs? args)
    {
        if (args != null)
        {
            // Execute on separate thread, so we won't block the periodic refresh in case the handler logic is expensive.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    ApplicationsFetched?.Invoke(this, args);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to handle {EventName} event.", nameof(ApplicationsFetched));
                }
            });
        }
    }

    internal async Task<ApplicationInfoCollection> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        _logger.LogDebug("Sending request to fetch applications.");

        ApplicationInfoCollection applications = string.IsNullOrWhiteSpace(clientOptions.RegistryRefreshSingleVipAddress)
            ? await _eurekaClient.GetApplicationsAsync(cancellationToken)
            : await _eurekaClient.GetByVipAsync(clientOptions.RegistryRefreshSingleVipAddress, cancellationToken);

        _logger.LogDebug("Full registry fetch succeeded with {Count} applications.", applications.RegisteredApplications.Count);
        return applications;
    }

    internal async Task<ApplicationInfoCollection> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
    {
        ApplicationInfoCollection delta;

        try
        {
            _logger.LogDebug("Sending request to fetch applications delta.");
            delta = await _eurekaClient.GetDeltaAsync(cancellationToken);
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogDebug(exception, "Failed to fetch registry delta. Trying full fetch.");
            return await FetchFullRegistryAsync(cancellationToken);
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

        _logger.LogDebug("Registry delta fetch succeeded with {Count} changes.", delta.RegisteredApplications.Count);
        _remoteApps.AppsHashCode = delta.AppsHashCode;
        return _remoteApps;
    }

    internal async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        if (_clientOptionsMonitor.CurrentValue.Health.CheckEnabled)
        {
            if (_appInfoManager.Instance.Status == InstanceStatus.Starting)
            {
                _logger.LogDebug("Skipping health check handler in starting state.");
                return;
            }

            try
            {
                InstanceStatus aggregatedStatus = await HealthCheckHandler.GetStatusAsync(_hasFirstHeartbeatCompleted, cancellationToken);
                _logger.LogDebug("Health check handler returned status {Status}.", aggregatedStatus);

                InstanceInfo snapshot = _appInfoManager.Instance;

                if (aggregatedStatus != snapshot.Status)
                {
                    _logger.LogDebug("Changing instance status from {LocalStatus} to {RemoteStatus}.", snapshot.Status, aggregatedStatus);
                    _appInfoManager.UpdateStatusWithoutRaisingEvent(aggregatedStatus);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Failed to determine health status.");
            }
        }
    }

    private void UpdateLastRemoteInstanceStatusFromCache()
    {
        InstanceInfo snapshot = _appInfoManager.Instance;
        ApplicationInfo? app = GetApplication(snapshot.AppName);
        InstanceInfo? remoteInstance = app?.GetInstance(snapshot.InstanceId);

        if (remoteInstance != null)
        {
            if (remoteInstance.EffectiveStatus != snapshot.EffectiveStatus)
            {
                _logger.LogWarning("Remote instance status {RemoteStatus} differs from local status {LocalStatus}.", remoteInstance.EffectiveStatus,
                    snapshot.EffectiveStatus);
            }

            // We have ownership of the local instance, so don't take the remote status.
            _lastRemoteInstanceStatus = new NullableValueWrapper<InstanceStatus>(remoteInstance.EffectiveStatus);
        }
    }

    private async void HeartbeatAsyncTask()
    {
        if (!IsAlive)
        {
            return;
        }

        try
        {
            await RenewAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            if (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Periodic renew failed.");
            }
        }
    }

    private async void CacheRefreshAsyncTask()
    {
        if (!IsAlive)
        {
            return;
        }

        try
        {
            await FetchRegistryAsync(false, CancellationToken.None);
        }
        catch (Exception exception)
        {
            if (!exception.IsCancellation())
            {
                _logger.LogError(exception, "Periodic fetch of applications failed.");
            }
        }
    }

    /// <inheritdoc />
    public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        ISet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ApplicationInfo app in Applications.RegisteredApplications)
        {
            if (app.Instances.Count != 0)
            {
                names.Add(app.Name);
            }
        }

        return Task.FromResult(names);
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        IReadOnlyList<InstanceInfo> nonSecureInstances = GetInstancesByVipAddress(serviceId, false);
        IReadOnlyList<InstanceInfo> secureInstances = GetInstancesByVipAddress(serviceId, true);

        InstanceInfo[] instances = secureInstances.Concat(nonSecureInstances).DistinctBy(instance => instance.InstanceId).ToArray();
        IList<IServiceInstance> serviceInstances = instances.Select(instance => new EurekaServiceInstance(instance)).Cast<IServiceInstance>().ToArray();

        _logger.LogDebug("Returning {Count} service instances: {ServiceInstances}", serviceInstances.Count,
            string.Join(", ", serviceInstances.Select(instance => $"{instance.ServiceId}={instance.Uri}")));

        return Task.FromResult(serviceInstances);
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return new EurekaServiceInstance(_appInfoManager.Instance);
    }
}
