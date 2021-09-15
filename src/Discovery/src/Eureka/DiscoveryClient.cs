// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Task;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using T = System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka
{
    public class DiscoveryClient : IEurekaClient
    {
        protected Timer _heartBeatTimer;
        protected Timer _cacheRefreshTimer;
        protected volatile Applications _localRegionApps;
        protected long _registryFetchCounter = 0;
        protected IEurekaHttpClient _httpClient;
        protected Random _random = new Random();
        protected ILogger _logger;
        protected ILogger _regularLogger;
        protected ILogger _startupLogger;
        protected int _shutdown = 0;
        protected ApplicationInfoManager _appInfoManager;

        public long LastGoodHeartbeatTimestamp { get; internal set; }

        public long LastGoodFullRegistryFetchTimestamp { get; internal set; }

        public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }

        public long LastGoodRegistryFetchTimestamp { get; internal set; }

        public long LastGoodRegisterTimestamp { get; internal set; }

        public InstanceStatus LastRemoteInstanceStatus { get; internal set; } = InstanceStatus.UNKNOWN;

        public IEurekaHttpClient HttpClient => _httpClient;

        public Applications Applications
        {
            get => _localRegionApps;

            internal set => _localRegionApps = value;
        }

        private readonly IEurekaClientConfig _config;

        public virtual IEurekaClientConfig ClientConfig => _config;

        public IHealthCheckHandler HealthCheckHandler { get; set; }

        public DiscoveryClient(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
            : this(ApplicationInfoManager.Instance, logFactory)
        {
            _config = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));

            _httpClient = httpClient;

            if (_httpClient == null)
            {
                _httpClient = new EurekaHttpClient(clientConfig, logFactory);
            }

            Initialize();
        }

        // Constructor used by Dependency Injection
        protected DiscoveryClient(ApplicationInfoManager appInfoManager, ILoggerFactory logFactory = null)
        {
            _appInfoManager = appInfoManager;
            _regularLogger = (ILogger)logFactory?.CreateLogger<DiscoveryClient>() ?? NullLogger.Instance;
            _startupLogger = logFactory?.CreateLogger("Startup." + this.GetType().FullName) ?? NullLogger.Instance;
        }

        public Application GetApplication(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            var apps = Applications;
            if (apps != null)
            {
                return apps.GetRegisteredApplication(appName);
            }

            return null;
        }

        public IList<InstanceInfo> GetInstanceById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            var results = new List<InstanceInfo>();

            var apps = Applications;
            if (apps == null)
            {
                return results;
            }

            var regApps = apps.GetRegisteredApplications();
            foreach (var app in regApps)
            {
                var instance = app.GetInstance(id);
                if (instance != null)
                {
                    results.Add(instance);
                }
            }

            return results;
        }

        public IList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure)
        {
            if (string.IsNullOrEmpty(vipAddress))
            {
                throw new ArgumentException(nameof(vipAddress));
            }

            var results = new List<InstanceInfo>();

            var apps = Applications;
            if (apps == null)
            {
                return results;
            }

            if (secure)
            {
                return apps.GetInstancesBySecureVirtualHostName(vipAddress);
            }
            else
            {
                return apps.GetInstancesByVirtualHostName(vipAddress);
            }
        }

        public IList<InstanceInfo> GetInstancesByVipAddressAndAppName(string vipAddress, string appName, bool secure)
        {
            IList<InstanceInfo> result = new List<InstanceInfo>();
            if (vipAddress == null && appName == null)
            {
                throw new ArgumentNullException("vipAddress and appName both null");
            }
            else if (vipAddress != null && appName == null)
            {
                return GetInstancesByVipAddress(vipAddress, secure);
            }
            else if (vipAddress == null)
            {
                // note: if appName were null, we would not get into this block
                var application = GetApplication(appName);
                if (application != null)
                {
                    result = application.Instances;
                }

                return result;
            }

            foreach (var app in _localRegionApps.GetRegisteredApplications())
            {
                foreach (var instance in app.Instances)
                {
                    string instanceVipAddress;
                    if (secure)
                    {
                        instanceVipAddress = instance.SecureVipAddress;
                    }
                    else
                    {
                        instanceVipAddress = instance.VipAddress;
                    }

                    if (vipAddress.Equals(instanceVipAddress, StringComparison.OrdinalIgnoreCase) &&
                        appName.Equals(instance.AppName, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(instance);
                    }
                }
            }

            return result;
        }

        public InstanceInfo GetNextServerFromEureka(string vipAddress, bool secure)
        {
            if (string.IsNullOrEmpty(vipAddress))
            {
                throw new ArgumentException(nameof(vipAddress));
            }

            var results = GetInstancesByVipAddress(vipAddress, secure);
            if (results.Count == 0)
            {
                return null;
            }

            var index = _random.Next() % results.Count;
            return results[index];
        }

        public virtual async T.Task ShutdownAsync()
        {
            var shutdown = Interlocked.Exchange(ref _shutdown, 1);
            if (shutdown > 0)
            {
                return;
            }

            if (_cacheRefreshTimer != null)
            {
                _cacheRefreshTimer.Dispose();
                _cacheRefreshTimer = null;
            }

            if (_heartBeatTimer != null)
            {
                _heartBeatTimer.Dispose();
                _heartBeatTimer = null;
            }

            if (ClientConfig.ShouldOnDemandUpdateStatusChange)
            {
                _appInfoManager.StatusChangedEvent -= Instance_StatusChangedEvent;
            }

            if (ClientConfig.ShouldRegisterWithEureka)
            {
                var info = _appInfoManager.InstanceInfo;
                if (info != null)
                {
                    info.Status = InstanceStatus.DOWN;
                    var result = await UnregisterAsync().ConfigureAwait(false);
                    if (!result)
                    {
                        _logger.LogWarning("Unregister failed during Shutdown");
                    }
                }
            }
        }

        public InstanceStatus GetInstanceRemoteStatus() => InstanceStatus.UNKNOWN;

        internal Timer HeartBeatTimer => _heartBeatTimer;

        internal Timer CacheRefreshTimer => _cacheRefreshTimer;

        internal async void Instance_StatusChangedEvent(object sender, StatusChangedArgs args)
        {
            var info = _appInfoManager.InstanceInfo;
            if (info != null)
            {
                _logger.LogDebug(
                    "Instance_StatusChangedEvent {previousStatus}, {currentStatus}, {instanceId}, {dirty}",
                    args.Previous,
                    args.Current,
                    args.InstanceId,
                    info.IsDirty);

                if (info.IsDirty)
                {
                    try
                    {
                        var result = await RegisterAsync().ConfigureAwait(false);
                        if (result)
                        {
                            info.IsDirty = false;
                            _logger.LogInformation("Instance_StatusChangedEvent RegisterAsync Succeed");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Instance_StatusChangedEvent RegisterAsync Failed");
                    }
                }
            }
        }

        internal long RegistryFetchCounter { get; set; }

        protected internal Timer StartTimer(string name, int interval, Action task)
        {
            var timedTask = new TimedTask(name, task);
            var timer = new Timer(timedTask.Run, null, interval, interval);
            return timer;
        }

        protected internal async T.Task<bool> FetchRegistryAsync(bool fullUpdate)
        {
            Applications fetched;
            try
            {
                if (fullUpdate ||
                    !string.IsNullOrEmpty(ClientConfig.RegistryRefreshSingleVipAddress) ||
                    ClientConfig.ShouldDisableDelta ||
                    _localRegionApps.GetRegisteredApplications().Count == 0)
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
                _logger.LogError(e, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}", new Uri(ClientConfig.EurekaServerServiceUrls).ToMaskedString());
                return false;
            }

            if (fetched != null)
            {
                _localRegionApps = fetched;
                _localRegionApps.ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances;
                LastGoodRegistryFetchTimestamp = DateTime.UtcNow.Ticks;

                // Notify about cache refresh before updating the instance remote status
                // onCacheRefreshed();

                //// Update remote status based on refreshed data held in the cache
                UpdateInstanceRemoteStatus();

                _logger.LogDebug("FetchRegistry succeeded");
                return true;
            }

            _logger.LogDebug("FetchRegistry failed");
            return false;
        }

        protected internal async T.Task<bool> UnregisterAsync()
        {
            var inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            try
            {
                var resp = await HttpClient.CancelAsync(inst.AppName, inst.InstanceId).ConfigureAwait(false);
                _logger.LogDebug("Unregister {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unregister Failed");
            }

            _logger.LogDebug("Unregister failed");
            return false;
        }

        protected internal async T.Task<bool> RegisterAsync()
        {
            var inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            try
            {
                var resp = await HttpClient.RegisterAsync(inst).ConfigureAwait(false);
                var result = resp.StatusCode == HttpStatusCode.NoContent;
                _logger.LogDebug("Register {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                if (result)
                {
                    LastGoodRegisterTimestamp = System.DateTime.UtcNow.Ticks;
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Register Failed");
            }

            _logger.LogDebug("Register failed");
            return false;
        }

        protected internal async T.Task<bool> RenewAsync()
        {
            var inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            RefreshInstanceInfo();

            if (inst.IsDirty)
            {
                await RegisterDirtyInstanceInfo(inst).ConfigureAwait(false);
            }

            try
            {
                var resp = await HttpClient.SendHeartBeatAsync(inst.AppName, inst.InstanceId, inst, InstanceStatus.UNKNOWN).ConfigureAwait(false);
                _logger.LogDebug("Renew {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Eureka heartbeat came back with 404 status. This could happen if Eureka was offline during app startup. Attempting to (re)register now.");
                    return await RegisterAsync().ConfigureAwait(false);
                }

                var result = resp.StatusCode == HttpStatusCode.OK;
                if (result)
                {
                    LastGoodHeartbeatTimestamp = System.DateTime.UtcNow.Ticks;
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Renew Failed");
            }

            _logger.LogDebug("Renew failed");
            return false;
        }

        protected internal async T.Task<Applications> FetchFullRegistryAsync()
        {
            var startingCounter = _registryFetchCounter;
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

            _logger.LogDebug(
                "FetchFullRegistry returned: {StatusCode}, {Response}",
                resp.StatusCode,
                (resp.Response != null) ? resp.Response.ToString() : "null");
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                fetched = resp.Response;
            }

            if (fetched != null && Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
            {
                // Log
                LastGoodFullRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
                return fetched;
            }
            else
            {
                _logger.LogWarning("FetchFullRegistry discarding fetch, race condition");
            }

            _logger.LogDebug("FetchFullRegistry failed");
            return null;
        }

        protected internal async T.Task<Applications> FetchRegistryDeltaAsync()
        {
            var startingCounter = _registryFetchCounter;
            Applications delta = null;

            var resp = await HttpClient.GetDeltaAsync().ConfigureAwait(false);
            _logger.LogDebug("FetchRegistryDelta returned: {StatusCode}", resp.StatusCode);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                delta = resp.Response;
            }

            if (delta == null)
            {
                // Log
                return await FetchFullRegistryAsync().ConfigureAwait(false);
            }

            if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
            {
                _localRegionApps.UpdateFromDelta(delta);
                var hashCode = _localRegionApps.ComputeHashCode();
                if (!hashCode.Equals(delta.AppsHashCode))
                {
                    _logger.LogWarning($"FetchRegistryDelta discarding delta, hashcodes mismatch: {hashCode}!={delta.AppsHashCode}");
                    return await FetchFullRegistryAsync().ConfigureAwait(false);
                }
                else
                {
                    _localRegionApps.AppsHashCode = delta.AppsHashCode;
                    LastGoodDeltaRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
                    return _localRegionApps;
                }
            }

            _logger.LogDebug("FetchRegistryDelta failed");
            return null;
        }

        protected internal void RefreshInstanceInfo()
        {
            var info = _appInfoManager.InstanceInfo;
            if (info == null)
            {
                return;
            }

            _appInfoManager.RefreshLeaseInfo();

            InstanceStatus? status = null;
            if (IsHealthCheckHandlerEnabled())
            {
                try
                {
                    status = HealthCheckHandler.GetStatus(info.Status);
                    _logger.LogDebug("RefreshInstanceInfo called, returning {status}", status);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        e,
                        "RefreshInstanceInfo HealthCheck handler. App: {Application}, Instance: {Instance} marked DOWN",
                        info.AppName,
                        info.InstanceId);
                    status = InstanceStatus.DOWN;
                }
            }

            if (status.HasValue)
            {
                _appInfoManager.InstanceStatus = status.Value;
            }
        }

        protected internal async T.Task<bool> RegisterDirtyInstanceInfo(InstanceInfo inst)
        {
            var regResult = await RegisterAsync().ConfigureAwait(false);
            _logger.LogDebug("Register dirty InstanceInfo returned {status}", regResult);
            if (regResult)
            {
                inst.IsDirty = false;
            }

            return regResult;
        }

        protected void Initialize() => InitializeAsync().GetAwaiter().GetResult();

        protected async T.Task InitializeAsync()
        {
            Interlocked.Exchange(ref _logger, _startupLogger);
            _localRegionApps = new Applications
            {
                ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances
            };

            // TODO: add Enabled to IEurekaClientConfig
            var eurekaClientConfig = ClientConfig as EurekaClientConfig;
            if (!eurekaClientConfig.Enabled || (!ClientConfig.ShouldRegisterWithEureka && !ClientConfig.ShouldFetchRegistry))
            {
                return;
            }

            if (ClientConfig.ShouldRegisterWithEureka && _appInfoManager.InstanceInfo != null)
            {
                if (!await RegisterAsync().ConfigureAwait(false))
                {
                    _logger.LogInformation("Initial Registration failed.");
                }

                _logger.LogInformation("Starting HeartBeat");
                var intervalInMilli = _appInfoManager.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
                _heartBeatTimer = StartTimer("HeartBeat", intervalInMilli, HeartBeatTaskAsync);
                if (ClientConfig.ShouldOnDemandUpdateStatusChange)
                {
                    _appInfoManager.StatusChangedEvent += Instance_StatusChangedEvent;
                }
            }

            if (ClientConfig.ShouldFetchRegistry)
            {
                await FetchRegistryAsync(true).ConfigureAwait(false);
                var intervalInMilli = ClientConfig.RegistryFetchIntervalSeconds * 1000;
                _cacheRefreshTimer = StartTimer("Query", intervalInMilli, CacheRefreshTaskAsync);
            }

            Interlocked.Exchange(ref _logger, _regularLogger);
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
            var info = _appInfoManager?.InstanceInfo;

            if (info != null && !string.IsNullOrEmpty(info.AppName))
            {
                var app = GetApplication(info.AppName);
                if (app != null)
                {
                    var remoteInstanceInfo = app.GetInstance(info.InstanceId);
                    if (remoteInstanceInfo != null)
                    {
                        LastRemoteInstanceStatus = remoteInstanceInfo.Status;
                        return;
                    }
                }

                LastRemoteInstanceStatus = InstanceStatus.UNKNOWN;
            }
        }

        // both of these should fire and forget on execution but log failures
#pragma warning disable S3168 // "async" methods should not return "void"
        private async void HeartBeatTaskAsync()
        {
            if (_shutdown > 0)
            {
                return;
            }

            var result = await RenewAsync().ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("HeartBeat failed");
            }
        }

        private async void CacheRefreshTaskAsync()
        {
            if (_shutdown > 0)
            {
                return;
            }

            var result = await FetchRegistryAsync(false).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogError("CacheRefresh failed");
            }
        }
#pragma warning restore S3168 // "async" methods should not return "void"
    }
}
