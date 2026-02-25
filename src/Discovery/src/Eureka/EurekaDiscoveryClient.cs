// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

// Justification: See the doc-comments on IDiscoveryClient.ShutdownAsync.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// A discovery client for
/// <see href="https://spring.io/guides/gs/service-registration-and-discovery/">
/// Spring Cloud Eureka
/// </see>
/// .
/// </summary>
public sealed partial class EurekaDiscoveryClient : IDiscoveryClient
{
    private readonly EurekaApplicationInfoManager _appInfoManager;
    private readonly EurekaClient _eurekaClient;
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly HealthCheckHandlerProvider _healthCheckHandlerProvider;
    private readonly TimeProvider _timeProvider;
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
        IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, HealthCheckHandlerProvider healthCheckHandlerProvider, TimeProvider timeProvider,
        ILogger<EurekaDiscoveryClient> logger)
    {
        ArgumentNullException.ThrowIfNull(appInfoManager);
        ArgumentNullException.ThrowIfNull(eurekaClient);
        ArgumentNullException.ThrowIfNull(clientOptionsMonitor);
        ArgumentNullException.ThrowIfNull(healthCheckHandlerProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _appInfoManager = appInfoManager;
        _eurekaClient = eurekaClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _healthCheckHandlerProvider = healthCheckHandlerProvider;
        _timeProvider = timeProvider;
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
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
                    // Justification: Async calls from a constructor are not possible. To fix this, an alternate design is needed.
                    RegisterAsync(false, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
                }
                catch (Exception exception)
                {
                    LogInitialRegistrationFailed(exception);
                }

                LogStartingHeartbeatTimer();
                _heartbeatTimer = StartTimer(leaseRenewalInterval.Value, HeartbeatAsyncTask);

                _appInfoManager.InstanceChanged += AppInfoManagerOnInstanceChanged;
            }
        }

        if (clientOptions.ShouldFetchRegistry && clientOptions.RegistryFetchInterval > TimeSpan.Zero)
        {
            try
            {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
                // Justification: Async calls from a constructor are not possible. To fix this, an alternate design is needed.
                FetchRegistryAsync(true, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
            }
            catch (Exception exception)
            {
                LogInitialFetchRegistryFailed(exception);
            }

            LogStartingCacheRefreshTimer();
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

    internal IReadOnlyList<InstanceInfo> GetInstancesByVipAddress(string vipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vipAddress);

        return Applications.GetInstancesByVipAddress(vipAddress);
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
            LogDeregisterFailedDuringShutdown(exception);
        }

        _appInfoManager.Dispose();
        _registerUnregisterAsyncLock.Dispose();
        _registryFetchAsyncLock.Dispose();
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void AppInfoManagerOnInstanceChanged(object? sender, InstanceChangedEventArgs args)
    {
        if (!IsAlive)
        {
            return;
        }

        try
        {
            LogInstanceChangedEvent(args.NewInstance, args.PreviousInstance);

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
                LogFailedToHandleEvent(exception, nameof(EurekaApplicationInfoManager.InstanceChanged));
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
                LogRegistering(snapshot.AppName, snapshot.InstanceId);
                await _eurekaClient.RegisterAsync(snapshot, cancellationToken);
                LogRegistrationSucceeded(snapshot.AppName, snapshot.InstanceId);

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

            LogDeregistering(snapshot.AppName, snapshot.InstanceId);
            await _eurekaClient.DeregisterAsync(snapshot.AppName, snapshot.InstanceId, cancellationToken);
            LogDeregistrationSucceeded(snapshot.AppName, snapshot.InstanceId);
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

            LogSendingHeartbeat(snapshot.AppName, snapshot.InstanceId);
            await _eurekaClient.HeartbeatAsync(snapshot.AppName, snapshot.InstanceId, snapshot.LastDirtyTimeUtc, cancellationToken);
            LogHeartbeatSucceeded(snapshot.AppName, snapshot.InstanceId);

            _lastGoodHeartbeatTimeUtc = new NullableValueWrapper<DateTime>(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (EurekaTransportException exception)
        {
            LogHeartbeatFailed(exception);

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
                clientOptions.IsFetchDeltaDisabled || _remoteApps.Count == 0;

            ApplicationInfoCollection fetched =
                requireFullFetch ? await FetchFullRegistryAsync(cancellationToken) : await FetchRegistryDeltaAsync(cancellationToken);

            _remoteApps = fetched;
            _remoteApps.ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances;
            _lastGoodRegistryFetchTimeUtc = new NullableValueWrapper<DateTime>(_timeProvider.GetUtcNow().UtcDateTime);

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
                    LogFailedToHandleEvent(exception, nameof(ApplicationsFetched));
                }
            });
        }
    }

    internal async Task<ApplicationInfoCollection> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        LogFetchingApplications();

        ApplicationInfoCollection applications = string.IsNullOrWhiteSpace(clientOptions.RegistryRefreshSingleVipAddress)
            ? await _eurekaClient.GetApplicationsAsync(cancellationToken)
            : await _eurekaClient.GetByVipAsync(clientOptions.RegistryRefreshSingleVipAddress, cancellationToken);

        LogFullRegistryFetchSucceeded(applications.Count);
        return applications;
    }

    internal async Task<ApplicationInfoCollection> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
    {
        ApplicationInfoCollection delta;

        try
        {
            LogFetchingApplicationsDelta();
            delta = await _eurekaClient.GetDeltaAsync(cancellationToken);
        }
        catch (EurekaTransportException exception)
        {
            LogFailedToFetchDelta(exception);
            return await FetchFullRegistryAsync(cancellationToken);
        }

        LogRegistryDeltaFetched();
        _remoteApps.UpdateFromDelta(delta);

        string hashCode = _remoteApps.ComputeHashCode();

        if (hashCode != delta.AppsHashCode)
        {
            LogDeltaHashCodeMismatch(hashCode, delta.AppsHashCode);

            return await FetchFullRegistryAsync(cancellationToken);
        }

        LogDeltaFetchSucceeded(delta.Count);
        _remoteApps.AppsHashCode = delta.AppsHashCode;
        return _remoteApps;
    }

    internal async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        if (_clientOptionsMonitor.CurrentValue.Health.CheckEnabled)
        {
            if (_appInfoManager.Instance.Status == InstanceStatus.Starting)
            {
                LogSkippingHealthCheck();
                return;
            }

            try
            {
                InstanceStatus aggregatedStatus = await HealthCheckHandler.GetStatusAsync(_hasFirstHeartbeatCompleted, cancellationToken);
                LogHealthCheckStatus(aggregatedStatus);

                InstanceInfo snapshot = _appInfoManager.Instance;

                if (aggregatedStatus != snapshot.Status)
                {
                    LogChangingInstanceStatus(snapshot.Status, aggregatedStatus);
                    _appInfoManager.UpdateStatusWithoutRaisingEvent(aggregatedStatus);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                LogFailedToDetermineHealthStatus(exception);
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
                LogRemoteStatusDiffers(remoteInstance.EffectiveStatus, snapshot.EffectiveStatus);
            }

            // We have ownership of the local instance, so don't take the remote status.
            _lastRemoteInstanceStatus = new NullableValueWrapper<InstanceStatus>(remoteInstance.EffectiveStatus);
        }
    }

    // ReSharper disable once AsyncVoidMethod
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
                LogPeriodicRenewFailed(exception);
            }
        }
    }

    // ReSharper disable once AsyncVoidMethod
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
                LogPeriodicFetchFailed(exception);
            }
        }
    }

    /// <inheritdoc />
    public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (ApplicationInfo app in Applications)
        {
            if (app.Instances.Count != 0)
            {
                names.Add(app.Name);
            }
        }

        return Task.FromResult<ISet<string>>(names);
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        IReadOnlyList<InstanceInfo> instances = GetInstancesByVipAddress(serviceId);
        IServiceInstance[] serviceInstances = instances.Select(instance => new EurekaServiceInstance(instance)).Cast<IServiceInstance>().ToArray();

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            string instanceNames = string.Join(", ", serviceInstances.Select(FormatServiceInstance));
            LogReturningServiceInstances(serviceInstances.Length, serviceId, instanceNames);
        }

        return Task.FromResult<IList<IServiceInstance>>(serviceInstances);
    }

    private static string FormatServiceInstance(IServiceInstance instance)
    {
        var builder = new StringBuilder();

        if (instance.SecureUri != null)
        {
            builder.Append(instance.SecureUri);
        }

        if (instance.NonSecureUri != null)
        {
            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(instance.NonSecureUri);
        }

        return $"{instance.InstanceId}={builder}";
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return new EurekaServiceInstance(_appInfoManager.Instance);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Initial registration failed.")]
    private partial void LogInitialRegistrationFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting heartbeat timer.")]
    private partial void LogStartingHeartbeatTimer();

    [LoggerMessage(Level = LogLevel.Information, Message = "Initial fetch registry failed.")]
    private partial void LogInitialFetchRegistryFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting applications cache refresh timer.")]
    private partial void LogStartingCacheRefreshTimer();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Deregister failed during shutdown.")]
    private partial void LogDeregisterFailedDuringShutdown(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Instance changed event handler invoked with new instance {NewInstance} and previous instance {PreviousInstance}.")]
    private partial void LogInstanceChangedEvent(InstanceInfo newInstance, InstanceInfo previousInstance);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to handle {EventName} event.")]
    private partial void LogFailedToHandleEvent(Exception exception, string eventName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registering {Application}/{Instance}.")]
    private partial void LogRegistering(string application, string instance);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Register {Application}/{Instance} succeeded.")]
    private partial void LogRegistrationSucceeded(string application, string instance);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deregistering {Application}/{Instance}.")]
    private partial void LogDeregistering(string application, string instance);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Deregister {Application}/{Instance} succeeded.")]
    private partial void LogDeregistrationSucceeded(string application, string instance);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending heartbeat for {Application}/{Instance}.")]
    private partial void LogSendingHeartbeat(string application, string instance);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Heartbeat for {Application}/{Instance} succeeded.")]
    private partial void LogHeartbeatSucceeded(string application, string instance);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Eureka heartbeat failed. This could happen if Eureka was offline during app startup. Attempting to (re)register now.")]
    private partial void LogHeartbeatFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending request to fetch applications.")]
    private partial void LogFetchingApplications();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Full registry fetch succeeded with {Count} applications.")]
    private partial void LogFullRegistryFetchSucceeded(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending request to fetch applications delta.")]
    private partial void LogFetchingApplicationsDelta();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to fetch registry delta. Trying full fetch.")]
    private partial void LogFailedToFetchDelta(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registry delta fetched, updating local cache.")]
    private partial void LogRegistryDeltaFetched();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Discarding fetched registry delta due to hash code mismatch between local {HashLocal} and remote {HashRemote}. Trying full fetch.")]
    private partial void LogDeltaHashCodeMismatch(string hashLocal, string? hashRemote);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registry delta fetch succeeded with {Count} changes.")]
    private partial void LogDeltaFetchSucceeded(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping health check handler in starting state.")]
    private partial void LogSkippingHealthCheck();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Health check handler returned status {Status}.")]
    private partial void LogHealthCheckStatus(InstanceStatus status);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Changing instance status from {LocalStatus} to {RemoteStatus}.")]
    private partial void LogChangingInstanceStatus(InstanceStatus? localStatus, InstanceStatus remoteStatus);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to determine health status.")]
    private partial void LogFailedToDetermineHealthStatus(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Remote instance status {RemoteStatus} differs from local status {LocalStatus}.")]
    private partial void LogRemoteStatusDiffers(InstanceStatus remoteStatus, InstanceStatus localStatus);

    [LoggerMessage(Level = LogLevel.Error, Message = "Periodic renew failed.")]
    private partial void LogPeriodicRenewFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Periodic fetch of applications failed.")]
    private partial void LogPeriodicFetchFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning {Count} service instances for '{ServiceId}': {ServiceInstances}.")]
    private partial void LogReturningServiceInstances(int count, string serviceId, string serviceInstances);
}
