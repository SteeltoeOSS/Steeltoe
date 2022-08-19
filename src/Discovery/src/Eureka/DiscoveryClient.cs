// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Task;
using Steeltoe.Discovery.Eureka.Transport;
using T = System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka;

public class DiscoveryClient : IEurekaClient
{
    protected Timer heartBeatTimer;
    protected Timer cacheRefreshTimer;
    protected volatile Applications localRegionApps;
    protected long registryFetchCounter;
    protected IEurekaHttpClient httpClient;
    protected Random random = new();
    protected ILogger logger;
    protected ILogger regularLogger;
    protected ILogger startupLogger;
    protected int shutdown;
    protected ApplicationInfoManager appInfoManager;

    internal Timer HeartBeatTimer => heartBeatTimer;

    internal Timer CacheRefreshTimer => cacheRefreshTimer;

    internal long RegistryFetchCounter { get; set; }

    public long LastGoodHeartbeatTimestamp { get; internal set; }

    public long LastGoodFullRegistryFetchTimestamp { get; internal set; }

    public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }

    public long LastGoodRegistryFetchTimestamp { get; internal set; }

    public long LastGoodRegisterTimestamp { get; internal set; }

    public InstanceStatus LastRemoteInstanceStatus { get; internal set; } = InstanceStatus.Unknown;

    public IEurekaHttpClient HttpClient => httpClient;

    public Applications Applications
    {
        get => localRegionApps;
        internal set => localRegionApps = value;
    }

    public virtual IEurekaClientConfig ClientConfig { get; }

    public IHealthCheckHandler HealthCheckHandler { get; set; }

    public event EventHandler<ApplicationsEventArgs> OnApplicationsChange;

    public DiscoveryClient(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
        : this(ApplicationInfoManager.Instance, logFactory)
    {
        ArgumentGuard.NotNull(clientConfig);

        ClientConfig = clientConfig;
        this.httpClient = httpClient ?? new EurekaHttpClient(clientConfig, logFactory);

        Initialize();
    }

    // Constructor used by Dependency Injection
    protected DiscoveryClient(ApplicationInfoManager appInfoManager, ILoggerFactory logFactory = null)
    {
        this.appInfoManager = appInfoManager;
        regularLogger = (ILogger)logFactory?.CreateLogger<DiscoveryClient>() ?? NullLogger.Instance;
        startupLogger = logFactory?.CreateLogger($"Startup.{GetType().FullName}") ?? NullLogger.Instance;
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

    public virtual async T.Task ShutdownAsync()
    {
        int shutdownValue = Interlocked.Exchange(ref shutdown, 1);

        if (shutdownValue > 0)
        {
            return;
        }

        if (cacheRefreshTimer != null)
        {
            cacheRefreshTimer.Dispose();
            cacheRefreshTimer = null;
        }

        if (heartBeatTimer != null)
        {
            heartBeatTimer.Dispose();
            heartBeatTimer = null;
        }

        if (ClientConfig.ShouldOnDemandUpdateStatusChange)
        {
            appInfoManager.StatusChanged -= HandleInstanceStatusChanged;
        }

        if (ClientConfig.ShouldRegisterWithEureka)
        {
            InstanceInfo info = appInfoManager.InstanceInfo;

            if (info != null)
            {
                info.Status = InstanceStatus.Down;
                bool result = await UnregisterAsync().ConfigureAwait(false);

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

    internal async void HandleInstanceStatusChanged(object sender, StatusChangedEventArgs args)
    {
        InstanceInfo info = appInfoManager.InstanceInfo;

        if (info != null)
        {
            logger.LogDebug("HandleInstanceStatusChanged {previousStatus}, {currentStatus}, {instanceId}, {dirty}", args.Previous, args.Current,
                args.InstanceId, info.IsDirty);

            if (info.IsDirty)
            {
                try
                {
                    bool result = await RegisterAsync().ConfigureAwait(false);

                    if (result)
                    {
                        info.IsDirty = false;
                        logger.LogInformation("HandleInstanceStatusChanged RegisterAsync Succeed");
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "HandleInstanceStatusChanged RegisterAsync Failed");
                }
            }
        }
    }

    protected internal Timer StartTimer(string name, int interval, Action task)
    {
        var timedTask = new TimedTask(name, task);
        var timer = new Timer(timedTask.Run, null, interval, interval);
        return timer;
    }

    protected internal async Task<bool> FetchRegistryAsync(bool fullUpdate)
    {
        Applications fetched;

        try
        {
            if (fullUpdate || !string.IsNullOrEmpty(ClientConfig.RegistryRefreshSingleVipAddress) || ClientConfig.ShouldDisableDelta ||
                localRegionApps.GetRegisteredApplications().Count == 0)
            {
                fetched = await FetchFullRegistryAsync().ConfigureAwait(false);
            }
            else
            {
                fetched = await FetchRegistryDeltaAsync().ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            // Log
            logger.LogError(e, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}",
                new Uri(ClientConfig.EurekaServerServiceUrls).ToMaskedString());

            return false;
        }

        if (fetched != null)
        {
            localRegionApps = fetched;
            localRegionApps.ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances;
            LastGoodRegistryFetchTimestamp = DateTime.UtcNow.Ticks;

            // Notify about cache refresh before updating the instance remote status
            // onCacheRefreshed();

            //// Update remote status based on refreshed data held in the cache
            UpdateInstanceRemoteStatus();

            logger.LogDebug("FetchRegistry succeeded");
            return true;
        }

        logger.LogDebug("FetchRegistry failed");
        return false;
    }

    protected internal async Task<bool> UnregisterAsync()
    {
        InstanceInfo inst = appInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        try
        {
            EurekaHttpResponse resp = await HttpClient.CancelAsync(inst.AppName, inst.InstanceId).ConfigureAwait(false);
            logger.LogDebug("Unregister {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
            return resp.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unregister Failed");
        }

        logger.LogDebug("Unregister failed");
        return false;
    }

    protected internal async Task<bool> RegisterAsync()
    {
        InstanceInfo inst = appInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        try
        {
            EurekaHttpResponse resp = await HttpClient.RegisterAsync(inst).ConfigureAwait(false);
            bool result = resp.StatusCode == HttpStatusCode.NoContent;
            logger.LogDebug("Register {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);

            if (result)
            {
                LastGoodRegisterTimestamp = DateTime.UtcNow.Ticks;
            }

            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Register Failed");
        }

        logger.LogDebug("Register failed");
        return false;
    }

    protected internal async Task<bool> RenewAsync()
    {
        InstanceInfo inst = appInfoManager.InstanceInfo;

        if (inst == null)
        {
            return false;
        }

        RefreshInstanceInfo();

        if (inst.IsDirty)
        {
            await RegisterDirtyInstanceInfoAsync(inst).ConfigureAwait(false);
        }

        try
        {
            EurekaHttpResponse<InstanceInfo> resp = await HttpClient.SendHeartBeatAsync(inst.AppName, inst.InstanceId, inst, InstanceStatus.Unknown)
                .ConfigureAwait(false);

            logger.LogDebug("Renew {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Eureka heartbeat came back with 404 status. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");

                return await RegisterAsync().ConfigureAwait(false);
            }

            bool result = resp.StatusCode == HttpStatusCode.OK;

            if (result)
            {
                LastGoodHeartbeatTimestamp = DateTime.UtcNow.Ticks;
            }

            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Renew Failed");
        }

        logger.LogDebug("Renew failed");
        return false;
    }

    protected internal async Task<Applications> FetchFullRegistryAsync()
    {
        long startingCounter = registryFetchCounter;
        Applications fetched = null;

        EurekaHttpResponse<Applications> resp;

        if (string.IsNullOrEmpty(ClientConfig.RegistryRefreshSingleVipAddress))
        {
            resp = await HttpClient.GetApplicationsAsync().ConfigureAwait(false);
        }
        else
        {
            resp = await HttpClient.GetVipAsync(ClientConfig.RegistryRefreshSingleVipAddress).ConfigureAwait(false);
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

    protected internal async Task<Applications> FetchRegistryDeltaAsync()
    {
        long startingCounter = registryFetchCounter;
        Applications delta = null;

        EurekaHttpResponse<Applications> resp = await HttpClient.GetDeltaAsync().ConfigureAwait(false);
        logger.LogDebug("FetchRegistryDelta returned: {StatusCode}", resp.StatusCode);

        if (resp.StatusCode == HttpStatusCode.OK)
        {
            delta = resp.Response;
        }

        if (delta == null)
        {
            // Log
            return await FetchFullRegistryAsync().ConfigureAwait(false);
        }

        if (Interlocked.CompareExchange(ref registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
        {
            localRegionApps.UpdateFromDelta(delta);
            string hashCode = localRegionApps.ComputeHashCode();

            if (!hashCode.Equals(delta.AppsHashCode))
            {
                logger.LogWarning($"FetchRegistryDelta discarding delta, hash codes mismatch: {hashCode}!={delta.AppsHashCode}");
                return await FetchFullRegistryAsync().ConfigureAwait(false);
            }

            localRegionApps.AppsHashCode = delta.AppsHashCode;
            LastGoodDeltaRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
            OnApplicationsChange?.Invoke(this, new ApplicationsEventArgs(delta));
            return localRegionApps;
        }

        logger.LogDebug("FetchRegistryDelta failed");
        return null;
    }

    protected internal void RefreshInstanceInfo()
    {
        InstanceInfo info = appInfoManager.InstanceInfo;

        if (info == null)
        {
            return;
        }

        appInfoManager.RefreshLeaseInfo();

        InstanceStatus? status = null;

        if (IsHealthCheckHandlerEnabled())
        {
            try
            {
                status = HealthCheckHandler.GetStatus(info.Status);
                logger.LogDebug("RefreshInstanceInfo called, returning {status}", status);
            }
            catch (Exception e)
            {
                logger.LogError(e, "RefreshInstanceInfo HealthCheck handler. App: {Application}, Instance: {Instance} marked DOWN", info.AppName,
                    info.InstanceId);

                status = InstanceStatus.Down;
            }
        }

        if (status.HasValue)
        {
            appInfoManager.InstanceStatus = status.Value;
        }
    }

    protected internal async Task<bool> RegisterDirtyInstanceInfoAsync(InstanceInfo inst)
    {
        bool regResult = await RegisterAsync().ConfigureAwait(false);
        logger.LogDebug("Register dirty InstanceInfo returned {status}", regResult);

        if (regResult)
        {
            inst.IsDirty = false;
        }

        return regResult;
    }

    protected void Initialize()
    {
        InitializeAsync().GetAwaiter().GetResult();
    }

    protected async T.Task InitializeAsync()
    {
        Interlocked.Exchange(ref logger, startupLogger);

        localRegionApps = new Applications
        {
            ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances
        };

        // TODO: add Enabled to IEurekaClientConfig
        var eurekaClientConfig = ClientConfig as EurekaClientConfig;

        if (!eurekaClientConfig.Enabled || (!ClientConfig.ShouldRegisterWithEureka && !ClientConfig.ShouldFetchRegistry))
        {
            return;
        }

        if (ClientConfig.ShouldRegisterWithEureka && appInfoManager.InstanceInfo != null)
        {
            if (!await RegisterAsync().ConfigureAwait(false))
            {
                logger.LogInformation("Initial Registration failed.");
            }

            logger.LogInformation("Starting HeartBeat");
            int intervalInMilliseconds = appInfoManager.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
            heartBeatTimer = StartTimer("HeartBeat", intervalInMilliseconds, HeartBeatTask);

            if (ClientConfig.ShouldOnDemandUpdateStatusChange)
            {
                appInfoManager.StatusChanged += HandleInstanceStatusChanged;
            }
        }

        if (ClientConfig.ShouldFetchRegistry)
        {
            await FetchRegistryAsync(true).ConfigureAwait(false);
            int intervalInMilliseconds = ClientConfig.RegistryFetchIntervalSeconds * 1000;
            cacheRefreshTimer = StartTimer("Query", intervalInMilliseconds, CacheRefreshTask);
        }

        Interlocked.Exchange(ref logger, regularLogger);
    }

    private bool IsHealthCheckHandlerEnabled()
    {
        if (ClientConfig is EurekaClientConfig config)
        {
            return config.HealthCheckEnabled && HealthCheckHandler != null;
        }

        return HealthCheckHandler != null;
    }

    private void UpdateInstanceRemoteStatus()
    {
        // Determine this instance's status for this app and set to UNKNOWN if not found
        InstanceInfo info = appInfoManager?.InstanceInfo;

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
#pragma warning disable S3168 // "async" methods should not return "void"
    private async void HeartBeatTask()
    {
        if (shutdown > 0)
        {
            return;
        }

        bool result = await RenewAsync().ConfigureAwait(false);

        if (!result)
        {
            logger.LogError("HeartBeat failed");
        }
    }

    private async void CacheRefreshTask()
    {
        if (shutdown > 0)
        {
            return;
        }

        bool result = await FetchRegistryAsync(false).ConfigureAwait(false);

        if (!result)
        {
            logger.LogError("CacheRefresh failed");
        }
    }
#pragma warning restore S3168 // "async" methods should not return "void"
}
