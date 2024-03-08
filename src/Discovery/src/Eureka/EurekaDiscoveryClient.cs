// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
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
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly ILogger<EurekaDiscoveryClient> _logger;
    private readonly EurekaClient _eurekaClient;
    private readonly EurekaApplicationInfoManager _appInfoManager;
    private int _shutdown;
    private volatile Applications _localRegionApps;
    private long _registryFetchCounter;

    internal Timer? HeartbeatTimer { get; private set; }
    internal Timer? CacheRefreshTimer { get; private set; }

    internal DateTime? LastGoodHeartbeatTimeUtc { get; private set; }
    internal DateTime? LastGoodRegistryFetchTimeUtc { get; private set; }
    internal InstanceStatus LastRemoteInstanceStatus { get; private set; } = InstanceStatus.Unknown;

    internal Applications Applications
    {
        get => _localRegionApps;
        set => _localRegionApps = value;
    }

    internal IHealthCheckHandler? HealthCheckHandler { get; set; }
    public string Description => "A discovery client for Spring Cloud Eureka.";
    public event EventHandler<ApplicationsEventArgs>? OnApplicationsChange;

    public EurekaDiscoveryClient(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        EurekaApplicationInfoManager appInfoManager, EurekaClient eurekaClient, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(appInfoManager);
        ArgumentGuard.NotNull(eurekaClient);
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        _appInfoManager = appInfoManager;
        _eurekaClient = eurekaClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _logger = loggerFactory.CreateLogger<EurekaDiscoveryClient>();

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        _localRegionApps = new Applications
        {
            ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances
        };

        if (!clientOptions.Enabled || clientOptions is { ShouldRegisterWithEureka: false, ShouldFetchRegistry: false })
        {
            return;
        }

        if (clientOptions.ShouldRegisterWithEureka && _appInfoManager.InstanceInfo.LeaseInfo?.RenewalInterval != null)
        {
            if (!RegisterAsync(CancellationToken.None).GetAwaiter().GetResult())
            {
                _logger.LogInformation("Initial registration failed.");
            }

            _logger.LogInformation("Starting Heartbeat");
            HeartbeatTimer = StartTimer(_appInfoManager.InstanceInfo.LeaseInfo.RenewalInterval.Value, HeartbeatTask);

            if (clientOptions.ShouldOnDemandUpdateStatusChange)
            {
                _appInfoManager.StatusChanged += HandleInstanceStatusChanged;
            }
        }

        if (clientOptions.ShouldFetchRegistry)
        {
            FetchRegistryAsync(true, CancellationToken.None).GetAwaiter().GetResult();
            CacheRefreshTimer = StartTimer(clientOptions.RegistryFetchInterval, CacheRefreshTask);
        }
    }

    internal void SetRegistryFetchCounter(long value)
    {
        _registryFetchCounter = value;
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

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        if (clientOptions.ShouldOnDemandUpdateStatusChange)
        {
            _appInfoManager.StatusChanged -= HandleInstanceStatusChanged;
        }

        if (clientOptions.ShouldRegisterWithEureka)
        {
            InstanceInfo instance = _appInfoManager.InstanceInfo;

            instance.Status = InstanceStatus.Down;
            bool result = await DeregisterAsync(cancellationToken);

            if (!result)
            {
                _logger.LogWarning("Deregister failed during shutdown.");
            }
        }
    }

    private async void HandleInstanceStatusChanged(object? sender, InstanceStatusChangedEventArgs args)
    {
        try
        {
            InstanceInfo instance = _appInfoManager.InstanceInfo;

            _logger.LogDebug("HandleInstanceStatusChanged {previousStatus}, {currentStatus}, {instanceId}, {dirty}", args.Previous, args.Current,
                args.InstanceId, instance.IsDirty);

            if (instance.IsDirty)
            {
                bool result = await RegisterAsync(CancellationToken.None);

                if (result)
                {
                    instance.IsDirty = false;
                    _logger.LogInformation("HandleInstanceStatusChanged RegisterAsync succeeded");
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "HandleInstanceStatusChanged failed");
        }
    }

    internal Timer StartTimer(TimeSpan interval, Action task)
    {
        // TODO: Change timer interval when config changed.

        var timedTask = new TimedTask(task);
        var timer = new Timer(_ => timedTask.Run(), null, interval, interval);
        return timer;
    }

    private async Task<bool> FetchRegistryAsync(bool fullUpdate, CancellationToken cancellationToken)
    {
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;
        Applications? fetched;

        try
        {
            if (fullUpdate || !string.IsNullOrEmpty(clientOptions.RegistryRefreshSingleVipAddress) || clientOptions.ShouldDisableDelta ||
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
            string uriString = clientOptions.EurekaServerServiceUrls != null ? new Uri(clientOptions.EurekaServerServiceUrls).ToMaskedString() : string.Empty;
            _logger.LogError(exception, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}", uriString);
            return false;
        }

        if (fetched != null)
        {
            _localRegionApps = fetched;
            _localRegionApps.ReturnUpInstancesOnly = clientOptions.ShouldFilterOnlyUpInstances;
            LastGoodRegistryFetchTimeUtc = DateTime.UtcNow;

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
        InstanceInfo instance = _appInfoManager.InstanceInfo;

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
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        try
        {
            await _eurekaClient.RegisterAsync(instance, cancellationToken);
            _logger.LogDebug("Register {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);

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
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        await RefreshInstanceInfoAsync(cancellationToken);

        if (instance.IsDirty)
        {
            await RegisterDirtyInstanceInfoAsync(instance, cancellationToken);
        }

        try
        {
            try
            {
                await _eurekaClient.HeartbeatAsync(instance.AppName, instance.InstanceId, instance.LastDirtyTimeUtc, cancellationToken);

                _logger.LogDebug("Renew {Application}/{Instance} succeeded.", instance.AppName, instance.InstanceId);

                LastGoodHeartbeatTimeUtc = DateTime.UtcNow;
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

    public async Task<Applications?> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        long startingCounter = _registryFetchCounter;
        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;
        Applications applications;

        try
        {
            applications = string.IsNullOrEmpty(clientOptions.RegistryRefreshSingleVipAddress)
                ? await _eurekaClient.GetApplicationsAsync(cancellationToken)
                : await _eurekaClient.GetVipAsync(clientOptions.RegistryRefreshSingleVipAddress, cancellationToken);

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

        OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(applications));

        return applications;
    }

    internal async Task<Applications?> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
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
            OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(delta));
            return _localRegionApps;
        }

        _logger.LogDebug("FetchRegistryDelta failed");
        return null;
    }

    internal async Task RefreshInstanceInfoAsync(CancellationToken cancellationToken)
    {
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        _appInfoManager.RefreshLeaseInfo();

        EurekaClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        if (clientOptions.Health.CheckEnabled && HealthCheckHandler != null)
        {
            InstanceStatus status;

            try
            {
                status = await HealthCheckHandler.GetStatusAsync(cancellationToken);
                _logger.LogDebug("RefreshInstanceInfo called, returning {status}", status);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger.LogError(exception, "RefreshInstanceInfo HealthCheck handler. App: {Application}, Instance: {Instance} marked DOWN", instance.AppName,
                    instance.InstanceId);

                status = InstanceStatus.Down;
            }

            _appInfoManager.InstanceStatus = status;
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

    private void UpdateInstanceRemoteStatus()
    {
        // Determine this instance's status for this app and set to UNKNOWN if not found
        InstanceInfo instance = _appInfoManager.InstanceInfo;

        if (!string.IsNullOrEmpty(instance.AppName))
        {
            Application? app = GetApplication(instance.AppName);

            if (app != null)
            {
                InstanceInfo? remoteInstanceInfo = app.GetInstance(instance.InstanceId);

                if (remoteInstanceInfo != null)
                {
                    LastRemoteInstanceStatus = remoteInstanceInfo.Status ?? InstanceStatus.Unknown;
                    return;
                }

                LastRemoteInstanceStatus = InstanceStatus.Unknown;
            }
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
        // TODO: Should also take SecureVipAddress into account. On duplicates, which takes precedence?
        // - https://stackoverflow.com/questions/45569978/discoveryclient-does-not-see-a-service-from-eureka
        // - https://github.com/spring-cloud/spring-cloud-netflix/issues/3763

        IList<InstanceInfo> instances = GetInstancesByVipAddress(serviceId, false);
        IList<IServiceInstance> serviceInstances = new List<IServiceInstance>();

        foreach (InstanceInfo instance in instances)
        {
            _logger.LogDebug($"GetInstances returning: {instance}");
            serviceInstances.Add(new EurekaServiceInstance(instance));
        }

        return Task.FromResult(serviceInstances);
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return new EurekaServiceInstance(_appInfoManager.InstanceInfo);
    }
}
