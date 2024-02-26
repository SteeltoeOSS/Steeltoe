// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Tasks;
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
    private readonly ILogger<EurekaDiscoveryClient> _logger;
    private readonly EurekaClient _eurekaClient;
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly IServiceInstance _thisInstance;
    internal readonly EurekaApplicationInfoManager AppInfoManager;
    private int _shutdown;
    private volatile Applications _localRegionApps;
    private long _registryFetchCounter;

    private EurekaClientOptions ClientOptions => _clientOptionsMonitor.CurrentValue;

    internal Timer HeartbeatTimer { get; private set; }
    internal Timer CacheRefreshTimer { get; private set; }

    public long LastGoodHeartbeatTimestamp { get; private set; }
    public long LastGoodFullRegistryFetchTimestamp { get; internal set; }
    public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }
    public long LastGoodRegistryFetchTimestamp { get; private set; }
    public long LastGoodRegisterTimestamp { get; internal set; }
    public InstanceStatus LastRemoteInstanceStatus { get; private set; } = InstanceStatus.Unknown;

    public Applications Applications
    {
        get => _localRegionApps;
        internal set => _localRegionApps = value;
    }

    public IHealthCheckHandler HealthCheckHandler { get; set; }
    public string Description => "A discovery client for Spring Cloud Eureka.";
    public event EventHandler<ApplicationsEventArgs> OnApplicationsChange;

    public EurekaDiscoveryClient(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        EurekaApplicationInfoManager appInfoManager, EurekaClient eurekaClient, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(appInfoManager);
        ArgumentGuard.NotNull(eurekaClient);
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        AppInfoManager = appInfoManager;
        _eurekaClient = eurekaClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EurekaDiscoveryClient>();
        _thisInstance = new ThisServiceInstance(instanceOptionsMonitor);

        _localRegionApps = new Applications
        {
            ReturnUpInstancesOnly = ClientOptions.ShouldFilterOnlyUpInstances
        };

        if (!ClientOptions.Enabled || (!ClientOptions.ShouldRegisterWithEureka && !ClientOptions.ShouldFetchRegistry))
        {
            return;
        }

        if (ClientOptions.ShouldRegisterWithEureka)
        {
            if (!RegisterAsync(CancellationToken.None).GetAwaiter().GetResult())
            {
                _logger.LogInformation("Initial registration failed.");
            }

            _logger.LogInformation("Starting Heartbeat");
            int intervalInMilliseconds = AppInfoManager.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
            HeartbeatTimer = StartTimer("Heartbeat", intervalInMilliseconds, HeartbeatTask);

            if (ClientOptions.ShouldOnDemandUpdateStatusChange)
            {
                AppInfoManager.StatusChanged += HandleInstanceStatusChanged;
            }
        }

        if (ClientOptions.ShouldFetchRegistry)
        {
            FetchRegistryAsync(true, CancellationToken.None).GetAwaiter().GetResult();
            int intervalInMilliseconds = ClientOptions.RegistryFetchIntervalSeconds * 1000;
            CacheRefreshTimer = StartTimer("Query", intervalInMilliseconds, CacheRefreshTask);
        }
    }

    internal void SetRegistryFetchCounter(long value)
    {
        _registryFetchCounter = value;
    }

    public Application GetApplication(string appName)
    {
        ArgumentGuard.NotNullOrEmpty(appName);

        Applications apps = Applications;

        if (apps != null)
        {
            return apps.GetRegisteredApplication(appName);
        }

        return null;
    }

    public IList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        Applications apps = Applications;

        if (apps == null)
        {
            return [];
        }

        if (secure)
        {
            return apps.GetInstancesBySecureVirtualHostName(vipAddress);
        }

        return apps.GetInstancesByVirtualHostName(vipAddress);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        int shutdownValue = Interlocked.Exchange(ref _shutdown, 1);

        if (shutdownValue > 0)
        {
            return;
        }

        AppInfoManager.InstanceStatus = InstanceStatus.Down;

        if (CacheRefreshTimer != null)
        {
            await CacheRefreshTimer.DisposeAsync();
            CacheRefreshTimer = null;
        }

        if (HeartbeatTimer != null)
        {
            await HeartbeatTimer.DisposeAsync();
            HeartbeatTimer = null;
        }

        if (ClientOptions.ShouldOnDemandUpdateStatusChange)
        {
            AppInfoManager.StatusChanged -= HandleInstanceStatusChanged;
        }

        if (ClientOptions.ShouldRegisterWithEureka)
        {
            InstanceInfo info = AppInfoManager.InstanceInfo;

            info.Status = InstanceStatus.Down;
            bool result = await DeregisterAsync(cancellationToken);

            if (!result)
            {
                _logger.LogWarning("Deregister failed during shutdown.");
            }
        }
    }

    private async void HandleInstanceStatusChanged(object sender, InstanceStatusChangedEventArgs args)
    {
        try
        {
            InstanceInfo info = AppInfoManager.InstanceInfo;

            _logger.LogDebug("HandleInstanceStatusChanged {previousStatus}, {currentStatus}, {instanceId}, {dirty}", args.Previous, args.Current,
                args.InstanceId, info.IsDirty);

            if (info.IsDirty)
            {
                bool result = await RegisterAsync(CancellationToken.None);

                if (result)
                {
                    info.IsDirty = false;
                    _logger.LogInformation("HandleInstanceStatusChanged RegisterAsync succeeded");
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "HandleInstanceStatusChanged failed");
        }
    }

    internal Timer StartTimer(string name, int interval, Action task)
    {
        var timedTask = new TimedTask(name, task);
        var timer = new Timer(timedTask.Run, null, interval, interval);
        return timer;
    }

    private async Task<bool> FetchRegistryAsync(bool fullUpdate, CancellationToken cancellationToken)
    {
        Applications fetched;

        try
        {
            if (fullUpdate || !string.IsNullOrEmpty(ClientOptions.RegistryRefreshSingleVipAddress) || ClientOptions.ShouldDisableDelta ||
                _localRegionApps.GetRegisteredApplications().Count == 0)
            {
                fetched = await FetchFullRegistryAsync(cancellationToken);
            }
            else
            {
                fetched = await FetchRegistryDeltaAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}",
                new Uri(ClientOptions.EurekaServerServiceUrls).ToMaskedString());

            return false;
        }

        if (fetched != null)
        {
            _localRegionApps = fetched;
            _localRegionApps.ReturnUpInstancesOnly = ClientOptions.ShouldFilterOnlyUpInstances;
            LastGoodRegistryFetchTimestamp = DateTime.UtcNow.Ticks;

            // Update remote status based on refreshed data held in the cache
            UpdateInstanceRemoteStatus();

            _logger.LogDebug("FetchRegistry succeeded");
            return true;
        }

        _logger.LogDebug("FetchRegistry failed");
        return false;
    }

    internal async Task<bool> DeregisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = AppInfoManager.InstanceInfo;

        try
        {
            await _eurekaClient.DeregisterAsync(instance.AppName, instance.InstanceId, cancellationToken);
            _logger.LogDebug("Deregister {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);
            return true;
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogError(exception, "Deregister {Application}/{Instance} failed.", instance.AppName, instance.InstanceId);
            return false;
        }
    }

    internal async Task<bool> RegisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = AppInfoManager.InstanceInfo;

        try
        {
            await _eurekaClient.RegisterAsync(instance, cancellationToken);
            _logger.LogDebug("Register {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);

            LastGoodRegisterTimestamp = DateTime.UtcNow.Ticks;
            return true;
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogError(exception, "Register {Application}/{Instance} failed.", instance.AppName, instance.InstanceId);
            return false;
        }
    }

    internal async Task<bool> RenewAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = AppInfoManager.InstanceInfo;

        await RefreshInstanceInfoAsync(cancellationToken);

        if (instance.IsDirty)
        {
            await RegisterDirtyInstanceInfoAsync(instance, cancellationToken);
        }

        try
        {
            try
            {
                await _eurekaClient.HeartbeatAsync(instance.AppName, instance.InstanceId, instance.Status,
                    new DateTime(instance.LastDirtyTimestamp, DateTimeKind.Utc), cancellationToken);

                _logger.LogDebug("Renew {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);

                LastGoodHeartbeatTimestamp = DateTime.UtcNow.Ticks;
                return true;
            }
            catch (EurekaTransportException exception)
            {
                _logger.LogWarning(exception,
                    "Eureka heartbeat failed. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");

                return await RegisterAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Renew {Application}/{Instance} failed.", instance.AppName, instance.InstanceId);
            return false;
        }
    }

    public async Task<Applications> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        long startingCounter = _registryFetchCounter;
        Applications applications;

        try
        {
            applications = string.IsNullOrEmpty(ClientOptions.RegistryRefreshSingleVipAddress)
                ? await _eurekaClient.GetApplicationsAsync(cancellationToken)
                : await _eurekaClient.GetVipAsync(ClientOptions.RegistryRefreshSingleVipAddress, cancellationToken);

            _logger.LogDebug("FetchFullRegistry succeeded.");
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogError(exception, "FetchFullRegistry failed.");
            return null;
        }

        if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) != startingCounter)
        {
            _logger.LogWarning("FetchFullRegistry discarding fetch, race condition");
            return null;
        }

        LastGoodFullRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
        OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(applications));

        return applications;
    }

    internal async Task<Applications> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
    {
        long startingCounter = _registryFetchCounter;
        Applications delta;

        try
        {
            delta = await _eurekaClient.GetDeltaAsync(cancellationToken);
            _logger.LogDebug("FetchRegistryDelta succeeded.");
        }
        catch (EurekaTransportException exception)
        {
            _logger.LogDebug(exception, "FetchRegistryDelta failed, trying full fetch.");
            return await FetchFullRegistryAsync(cancellationToken);
        }

        if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
        {
            _localRegionApps.UpdateFromDelta(delta);
            string hashCode = _localRegionApps.ComputeHashCode();

            if (hashCode != delta.AppsHashCode)
            {
                _logger.LogWarning($"FetchRegistryDelta discarding delta, hash codes mismatch: {hashCode}!={delta.AppsHashCode}");
                return await FetchFullRegistryAsync(cancellationToken);
            }

            _localRegionApps.AppsHashCode = delta.AppsHashCode;
            LastGoodDeltaRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
            OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(delta));
            return _localRegionApps;
        }

        _logger.LogDebug("FetchRegistryDelta failed");
        return null;
    }

    internal async Task RefreshInstanceInfoAsync(CancellationToken cancellationToken)
    {
        InstanceInfo info = AppInfoManager.InstanceInfo;

        AppInfoManager.RefreshLeaseInfo();

        if (IsHealthCheckHandlerEnabled())
        {
            InstanceStatus status;

            try
            {
                status = await HealthCheckHandler.GetStatusAsync(cancellationToken);
                _logger.LogDebug("RefreshInstanceInfo called, returning {status}", status);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "RefreshInstanceInfo HealthCheck handler. App: {Application}, Instance: {Instance} marked DOWN", info.AppName,
                    info.InstanceId);

                status = InstanceStatus.Down;
            }

            AppInfoManager.InstanceStatus = status;
        }
    }

    private async Task RegisterDirtyInstanceInfoAsync(InstanceInfo instance, CancellationToken cancellationToken)
    {
        bool result = await RegisterAsync(cancellationToken);
        _logger.LogDebug("Register dirty InstanceInfo returned {status}", result);

        if (result)
        {
            instance.IsDirty = false;
        }
    }

    private bool IsHealthCheckHandlerEnabled()
    {
        return ClientOptions.Health.CheckEnabled && HealthCheckHandler != null;
    }

    private void UpdateInstanceRemoteStatus()
    {
        // Determine this instance's status for this app and set to UNKNOWN if not found
        InstanceInfo info = AppInfoManager.InstanceInfo;

        if (!string.IsNullOrEmpty(info.AppName))
        {
            Application app = GetApplication(info.AppName);

            if (app != null)
            {
                InstanceInfo remoteInstanceInfo = app.GetInstance(info.InstanceId);

                if (remoteInstanceInfo != null)
                {
                    LastRemoteInstanceStatus = remoteInstanceInfo.Status;
                    return;
                }
            }

            LastRemoteInstanceStatus = InstanceStatus.Unknown;
        }
    }

    // both of these should fire and forget on execution but log failures
    private async void HeartbeatTask()
    {
        try
        {
            int shutdownValue = Interlocked.Add(ref _shutdown, 0);

            if (shutdownValue > 0)
            {
                return;
            }

            bool result = await RenewAsync(CancellationToken.None);

            if (!result)
            {
                _logger.LogError("Heartbeat failed");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Heartbeat failed");
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

            bool result = await FetchRegistryAsync(false, CancellationToken.None);

            if (!result)
            {
                _logger.LogError("CacheRefresh failed");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "CacheRefresh failed");
        }
    }

    /// <inheritdoc />
    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        Applications applications = Applications;

        if (applications == null)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

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
        IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
        IList<IServiceInstance> instances = new List<IServiceInstance>();

        foreach (InstanceInfo info in infos)
        {
            _logger.LogDebug($"GetInstances returning: {info}");
            instances.Add(new EurekaServiceInstance(info));
        }

        return Task.FromResult(instances);
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return _thisInstance;
    }
}
