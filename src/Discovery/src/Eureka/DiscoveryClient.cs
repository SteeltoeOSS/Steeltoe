// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Tasks;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

public class DiscoveryClient
{
    protected readonly ApplicationInfoManager AppInfoManager;
    private int _shutdown;
    protected Timer heartBeatTimer;
    protected Timer cacheRefreshTimer;
    protected volatile Applications localRegionApps;
    protected long registryFetchCounter;
    protected EurekaHttpClient httpClient;
    protected Random random = new();
    protected ILogger logger;
    protected ILogger regularLogger;
    protected ILogger startupLogger;

    internal Timer HeartBeatTimer => heartBeatTimer;

    internal Timer CacheRefreshTimer => cacheRefreshTimer;

    internal long RegistryFetchCounter { get; set; }

    public long LastGoodHeartbeatTimestamp { get; private set; }

    public long LastGoodFullRegistryFetchTimestamp { get; internal set; }

    public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }

    public long LastGoodRegistryFetchTimestamp { get; private set; }

    public long LastGoodRegisterTimestamp { get; internal set; }

    public InstanceStatus LastRemoteInstanceStatus { get; private set; } = InstanceStatus.Unknown;

    public EurekaHttpClient HttpClient => httpClient;

    public Applications Applications
    {
        get => localRegionApps;
        internal set => localRegionApps = value;
    }

    public virtual EurekaClientConfiguration ClientConfiguration { get; }

    public IHealthCheckHandler HealthCheckHandler { get; set; }

    public event EventHandler<ApplicationsEventArgs> OnApplicationsChange;

    public DiscoveryClient(EurekaClientConfiguration clientConfiguration, EurekaHttpClient httpClient = null, ILoggerFactory loggerFactory = null)
        : this(ApplicationInfoManager.Instance, loggerFactory)
    {
        ArgumentGuard.NotNull(clientConfiguration);

        ClientConfiguration = clientConfiguration;
        this.httpClient = httpClient ?? new EurekaHttpClient(clientConfiguration, loggerFactory);

        InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    // Constructor used by Dependency Injection
    protected DiscoveryClient(ApplicationInfoManager appInfoManager, ILoggerFactory loggerFactory = null)
    {
        AppInfoManager = appInfoManager;
        regularLogger = (ILogger)loggerFactory?.CreateLogger<DiscoveryClient>() ?? NullLogger.Instance;
        startupLogger = loggerFactory?.CreateLogger($"Startup.{GetType().FullName}") ?? NullLogger.Instance;
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

    public IList<InstanceInfo> GetInstanceById(string id)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        var results = new List<InstanceInfo>();

        Applications apps = Applications;

        if (apps == null)
        {
            return results;
        }

        IList<Application> regApps = apps.GetRegisteredApplications();

        foreach (Application app in regApps)
        {
            InstanceInfo instance = app.GetInstance(id);

            if (instance != null)
            {
                results.Add(instance);
            }
        }

        return results;
    }

    public IList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure)
    {
        ArgumentGuard.NotNullOrEmpty(vipAddress);

        var results = new List<InstanceInfo>();

        Applications apps = Applications;

        if (apps == null)
        {
            return results;
        }

        if (secure)
        {
            return apps.GetInstancesBySecureVirtualHostName(vipAddress);
        }

        return apps.GetInstancesByVirtualHostName(vipAddress);
    }

    public IList<InstanceInfo> GetInstancesByVipAddressAndAppName(string vipAddress, string appName, bool secure)
    {
        if (vipAddress == null && appName == null)
        {
            throw new ArgumentException($"{nameof(vipAddress)} and {nameof(appName)} cannot both be null.");
        }

        IList<InstanceInfo> result = new List<InstanceInfo>();

        if (vipAddress != null && appName == null)
        {
            return GetInstancesByVipAddress(vipAddress, secure);
        }

        if (vipAddress == null)
        {
            // note: if appName were null, we would not get into this block
            Application application = GetApplication(appName);

            if (application != null)
            {
                result = application.Instances;
            }

            return result;
        }

        foreach (Application app in localRegionApps.GetRegisteredApplications())
        {
            foreach (InstanceInfo instance in app.Instances)
            {
                string instanceVipAddress = secure ? instance.SecureVipAddress : instance.VipAddress;

                if (vipAddress.Equals(instanceVipAddress, StringComparison.OrdinalIgnoreCase) &&
                    appName.Equals(instance.AppName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(instance);
                }
            }
        }

        return result;
    }

    public InstanceInfo GetNextServerFromEureka(string virtualHostname, bool secure)
    {
        ArgumentGuard.NotNullOrEmpty(virtualHostname);

        IList<InstanceInfo> results = GetInstancesByVipAddress(virtualHostname, secure);

        if (results.Count == 0)
        {
            return null;
        }

        int index = random.Next() % results.Count;
        return results[index];
    }

    public virtual async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        int shutdownValue = Interlocked.Exchange(ref _shutdown, 1);

        if (shutdownValue > 0)
        {
            return;
        }

        if (cacheRefreshTimer != null)
        {
            await cacheRefreshTimer.DisposeAsync();
            cacheRefreshTimer = null;
        }

        if (heartBeatTimer != null)
        {
            await heartBeatTimer.DisposeAsync();
            heartBeatTimer = null;
        }

        if (ClientConfiguration.ShouldOnDemandUpdateStatusChange)
        {
            AppInfoManager.StatusChanged -= HandleInstanceStatusChanged;
        }

        if (ClientConfiguration.ShouldRegisterWithEureka)
        {
            InstanceInfo info = AppInfoManager.InstanceInfo;

            if (info != null)
            {
                info.Status = InstanceStatus.Down;
                bool result = await UnregisterAsync(cancellationToken);

                if (!result)
                {
                    logger.LogWarning("Unregister failed during Shutdown");
                }
            }
        }
    }

    public InstanceStatus GetInstanceRemoteStatus()
    {
        return InstanceStatus.Unknown;
    }

    private async void HandleInstanceStatusChanged(object sender, StatusChangedEventArgs args)
    {
        try
        {
            InstanceInfo info = AppInfoManager.InstanceInfo;

            if (info != null)
            {
                logger.LogDebug("HandleInstanceStatusChanged {previousStatus}, {currentStatus}, {instanceId}, {dirty}", args.Previous, args.Current,
                    args.InstanceId, info.IsDirty);

                if (info.IsDirty)
                {
                    bool result = await RegisterAsync(CancellationToken.None);

                    if (result)
                    {
                        info.IsDirty = false;
                        logger.LogInformation("HandleInstanceStatusChanged RegisterAsync succeeded");
                    }
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "HandleInstanceStatusChanged failed");
        }
    }

    protected internal Timer StartTimer(string name, int interval, Action task)
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
            if (fullUpdate || !string.IsNullOrEmpty(ClientConfiguration.RegistryRefreshSingleVipAddress) || ClientConfiguration.ShouldDisableDelta ||
                localRegionApps.GetRegisteredApplications().Count == 0)
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
            logger.LogError(exception, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}",
                new Uri(ClientConfiguration.EurekaServerServiceUrls).ToMaskedString());

            return false;
        }

        if (fetched != null)
        {
            localRegionApps = fetched;
            localRegionApps.ReturnUpInstancesOnly = ClientConfiguration.ShouldFilterOnlyUpInstances;
            LastGoodRegistryFetchTimestamp = DateTime.UtcNow.Ticks;

            //// Update remote status based on refreshed data held in the cache
            UpdateInstanceRemoteStatus();

            logger.LogDebug("FetchRegistry succeeded");
            return true;
        }

        logger.LogDebug("FetchRegistry failed");
        return false;
    }

    protected internal async Task<bool> UnregisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo inst = AppInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        try
        {
            EurekaHttpResponse resp = await HttpClient.CancelAsync(inst.AppName, inst.InstanceId, cancellationToken);
            logger.LogDebug("Unregister {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
            return resp.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            logger.LogError(exception, "Unregister Failed");
        }

        logger.LogDebug("Unregister failed");
        return false;
    }

    protected internal async Task<bool> RegisterAsync(CancellationToken cancellationToken)
    {
        InstanceInfo inst = AppInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        try
        {
            EurekaHttpResponse resp = await HttpClient.RegisterAsync(inst, cancellationToken);
            bool result = resp.StatusCode == HttpStatusCode.NoContent;
            logger.LogDebug("Register {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);

            if (result)
            {
                LastGoodRegisterTimestamp = DateTime.UtcNow.Ticks;
            }

            return result;
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            logger.LogError(exception, "Register Failed");
        }

        logger.LogDebug("Register failed");
        return false;
    }

    protected internal async Task<bool> RenewAsync(CancellationToken cancellationToken)
    {
        InstanceInfo inst = AppInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        await RefreshInstanceInfoAsync(cancellationToken);

        if (inst.IsDirty)
        {
            await RegisterDirtyInstanceInfoAsync(inst, cancellationToken);
        }

        try
        {
            EurekaHttpResponse<InstanceInfo> resp =
                await HttpClient.SendHeartBeatAsync(inst.AppName, inst.InstanceId, inst, InstanceStatus.Unknown, cancellationToken);

            logger.LogDebug("Renew {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Eureka heartbeat came back with 404 status. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");

                return await RegisterAsync(cancellationToken);
            }

            bool result = resp.StatusCode == HttpStatusCode.OK;

            if (result)
            {
                LastGoodHeartbeatTimestamp = DateTime.UtcNow.Ticks;
            }

            return result;
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            logger.LogError(exception, "Renew Failed");
        }

        logger.LogDebug("Renew failed");
        return false;
    }

    protected internal async Task<Applications> FetchFullRegistryAsync(CancellationToken cancellationToken)
    {
        long startingCounter = registryFetchCounter;
        Applications fetched = null;

        EurekaHttpResponse<Applications> resp;

        if (string.IsNullOrEmpty(ClientConfiguration.RegistryRefreshSingleVipAddress))
        {
            resp = await HttpClient.GetApplicationsAsync(cancellationToken);
        }
        else
        {
            resp = await HttpClient.GetVipAsync(ClientConfiguration.RegistryRefreshSingleVipAddress, cancellationToken);
        }

        logger.LogDebug("FetchFullRegistry returned: {StatusCode}, {Response}", resp.StatusCode, resp.Response != null ? resp.Response.ToString() : "null");

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            fetched = resp.Response;
        }

        if (fetched != null && Interlocked.CompareExchange(ref registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
        {
            // Log
            LastGoodFullRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
            OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(fetched));
            return fetched;
        }

        logger.LogWarning("FetchFullRegistry discarding fetch, race condition");

        logger.LogDebug("FetchFullRegistry failed");
        return null;
    }

    protected internal async Task<Applications> FetchRegistryDeltaAsync(CancellationToken cancellationToken)
    {
        long startingCounter = registryFetchCounter;
        Applications delta = null;

        EurekaHttpResponse<Applications> resp = await HttpClient.GetDeltaAsync(cancellationToken);
        logger.LogDebug("FetchRegistryDelta returned: {StatusCode}", resp.StatusCode);

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            delta = resp.Response;
        }

        if (delta == null)
        {
            // Log
            return await FetchFullRegistryAsync(cancellationToken);
        }

        if (Interlocked.CompareExchange(ref registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
        {
            localRegionApps.UpdateFromDelta(delta);
            string hashCode = localRegionApps.ComputeHashCode();

            if (hashCode != delta.AppsHashCode)
            {
                logger.LogWarning($"FetchRegistryDelta discarding delta, hash codes mismatch: {hashCode}!={delta.AppsHashCode}");
                return await FetchFullRegistryAsync(cancellationToken);
            }

            localRegionApps.AppsHashCode = delta.AppsHashCode;
            LastGoodDeltaRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
            OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(delta));
            return localRegionApps;
        }

        logger.LogDebug("FetchRegistryDelta failed");
        return null;
    }

    protected internal async Task RefreshInstanceInfoAsync(CancellationToken cancellationToken)
    {
        InstanceInfo info = AppInfoManager.InstanceInfo;

        if (info == null)
        {
            return;
        }

        AppInfoManager.RefreshLeaseInfo();

        InstanceStatus? status = null;

        if (IsHealthCheckHandlerEnabled())
        {
            try
            {
                status = await HealthCheckHandler.GetStatusAsync(cancellationToken);
                logger.LogDebug("RefreshInstanceInfo called, returning {status}", status);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                logger.LogError(exception, "RefreshInstanceInfo HealthCheck handler. App: {Application}, Instance: {Instance} marked DOWN", info.AppName,
                    info.InstanceId);

                status = InstanceStatus.Down;
            }
        }

        if (status.HasValue)
        {
            AppInfoManager.InstanceStatus = status.Value;
        }
    }

    private async Task RegisterDirtyInstanceInfoAsync(InstanceInfo inst, CancellationToken cancellationToken)
    {
        bool regResult = await RegisterAsync(cancellationToken);
        logger.LogDebug("Register dirty InstanceInfo returned {status}", regResult);

        if (regResult)
        {
            inst.IsDirty = false;
        }
    }

    protected async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref logger, startupLogger);

        localRegionApps = new Applications
        {
            ReturnUpInstancesOnly = ClientConfiguration.ShouldFilterOnlyUpInstances
        };

        if (ClientConfiguration is EurekaClientConfiguration eurekaClientConfig && (!eurekaClientConfig.Enabled ||
            (!ClientConfiguration.ShouldRegisterWithEureka && !ClientConfiguration.ShouldFetchRegistry)))
        {
            return;
        }

        if (ClientConfiguration.ShouldRegisterWithEureka && AppInfoManager.InstanceInfo != null)
        {
            if (!await RegisterAsync(cancellationToken))
            {
                logger.LogInformation("Initial Registration failed.");
            }

            logger.LogInformation("Starting HeartBeat");
            int intervalInMilliseconds = AppInfoManager.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
            heartBeatTimer = StartTimer("HeartBeat", intervalInMilliseconds, HeartBeatTask);

            if (ClientConfiguration.ShouldOnDemandUpdateStatusChange)
            {
                AppInfoManager.StatusChanged += HandleInstanceStatusChanged;
            }
        }

        if (ClientConfiguration.ShouldFetchRegistry)
        {
            await FetchRegistryAsync(true, cancellationToken);
            int intervalInMilliseconds = ClientConfiguration.RegistryFetchIntervalSeconds * 1000;
            cacheRefreshTimer = StartTimer("Query", intervalInMilliseconds, CacheRefreshTask);
        }

        Interlocked.Exchange(ref logger, regularLogger);
    }

    private bool IsHealthCheckHandlerEnabled()
    {
        if (ClientConfiguration is EurekaClientConfiguration configuration)
        {
            return configuration.HealthCheckEnabled && HealthCheckHandler != null;
        }

        return HealthCheckHandler != null;
    }

    private void UpdateInstanceRemoteStatus()
    {
        // Determine this instance's status for this app and set to UNKNOWN if not found
        InstanceInfo info = AppInfoManager?.InstanceInfo;

        if (info != null && !string.IsNullOrEmpty(info.AppName))
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
    private async void HeartBeatTask()
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
                logger.LogError("HeartBeat failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "HeartBeat failed");
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
                logger.LogError("CacheRefresh failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "CacheRefresh failed");
        }
    }
}
