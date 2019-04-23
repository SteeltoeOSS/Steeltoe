// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Task;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using T=System.Threading.Tasks;

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
        protected int _shutdown = 0;
        protected ApplicationInfoManager _appInfoManager;

        public long LastGoodHeartbeatTimestamp { get; internal set; }

        public long LastGoodFullRegistryFetchTimestamp { get; internal set; }

        public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }

        public long LastGoodRegistryFetchTimestamp { get; internal set; }

        public long LastGoodRegisterTimestamp { get; internal set; }

        public InstanceStatus LastRemoteInstanceStatus { get; internal set; } = InstanceStatus.UNKNOWN;

        public IEurekaHttpClient HttpClient
        {
            get
            {
                return _httpClient;
            }
        }

        public Applications Applications
        {
            get
            {
                return _localRegionApps;
            }

            internal set
            {
                _localRegionApps = value;
            }
        }

        private IEurekaClientConfig _config;

        public virtual IEurekaClientConfig ClientConfig
        {
            get
            {
                return _config;
            }
        }

        public IHealthCheckHandler HealthCheckHandler { get; set; }

        public DiscoveryClient(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            _appInfoManager = ApplicationInfoManager.Instance;
            _logger = logFactory?.CreateLogger<DiscoveryClient>();
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
            _logger = logFactory?.CreateLogger<DiscoveryClient>();
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

            List<InstanceInfo> results = new List<InstanceInfo>();

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

            List<InstanceInfo> results = new List<InstanceInfo>();

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
            else if (vipAddress == null && appName != null)
            {
                Application application = GetApplication(appName);
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
                    string instanceVipAddress = null;
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
            int shutdown = Interlocked.Exchange(ref _shutdown, 1);
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
                InstanceInfo info = _appInfoManager.InstanceInfo;
                if (info != null)
                {
                    info.Status = InstanceStatus.DOWN;
                    var result = await UnregisterAsync();
                    if (!result)
                    {
                        _logger?.LogWarning("Unregister failed during Shutdown");
                    }
                }
            }
        }

        public InstanceStatus GetInstanceRemoteStatus()
        {
            return InstanceStatus.UNKNOWN;
        }

        internal Timer HeartBeatTimer
        {
            get
            {
                return _heartBeatTimer;
            }
        }

        internal Timer CacheRefreshTimer
        {
            get
            {
                return _cacheRefreshTimer;
            }
        }

        internal async void Instance_StatusChangedEvent(object sender, StatusChangedArgs args)
        {
            InstanceInfo info = _appInfoManager.InstanceInfo;
            if (info != null)
            {
                _logger?.LogDebug(
                    "Instance_StatusChangedEvent {previousStatus}, {currentStatus}, {instanceId}, {dirty}",
                    args.Previous,
                    args.Current,
                    args.InstanceId,
                    info.IsDirty);

                if (info.IsDirty)
                {
                    try
                    {
                        var result = await RegisterAsync();
                        if (result)
                        {
                            info.IsDirty = false;
                            _logger?.LogInformation("Instance_StatusChangedEvent RegisterAsync Succeed");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Instance_StatusChangedEvent RegisterAsync Failed");
                    }
                }
            }
        }

        internal long RegistryFetchCounter
        {
            get
            {
                return _registryFetchCounter;
            }

            set
            {
                // Used for unit test
                _registryFetchCounter = value;
            }
        }

        protected internal Timer StartTimer(string name, int interval, Action task)
        {
            var timedTask = new TimedTask(name, task);
            var timer = new Timer(timedTask.Run, null, interval, interval);
            return timer;
        }

        protected internal async T.Task<bool> FetchRegistryAsync(bool fullUpdate)
        {
            Applications fetched = null;
            try
            {
                if (fullUpdate ||
                    !string.IsNullOrEmpty(ClientConfig.RegistryRefreshSingleVipAddress) ||
                    ClientConfig.ShouldDisableDelta ||
                    _localRegionApps.GetRegisteredApplications().Count == 0)
                {
                    fetched = await FetchFullRegistryAsync();
                }
                else
                {
                    fetched = await FetchRegistryDeltaAsync();
                }
            }
            catch (Exception e)
            {
                // Log
                _logger?.LogError(e, "FetchRegistry Failed for Eureka service urls: {EurekaServerServiceUrls}", ClientConfig.EurekaServerServiceUrls);
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

                _logger?.LogDebug("FetchRegistry succeeded");
                return true;
            }

            _logger?.LogDebug("FetchRegistry failed");
            return false;
        }

        protected internal async T.Task<bool> UnregisterAsync()
        {
            InstanceInfo inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            try
            {
                EurekaHttpResponse resp = await HttpClient.CancelAsync(inst.AppName, inst.InstanceId);
                _logger?.LogDebug("Unregister {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                return resp.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Unregister Failed");
            }

            _logger?.LogDebug("Unregister failed");
            return false;
        }

        protected internal async T.Task<bool> RegisterAsync()
        {
            InstanceInfo inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            try
            {
                EurekaHttpResponse resp = await HttpClient.RegisterAsync(inst);
                var result = resp.StatusCode == HttpStatusCode.NoContent;
                _logger?.LogDebug("Register {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                if (result)
                {
                    LastGoodRegisterTimestamp = System.DateTime.UtcNow.Ticks;
                }

                return result;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Register Failed");
            }

            _logger?.LogDebug("Register failed");
            return false;
        }

        protected internal async T.Task<bool> RenewAsync()
        {
            InstanceInfo inst = _appInfoManager.InstanceInfo;
            if (inst == null)
            {
                return false;
            }

            RefreshInstanceInfo();

            if (inst.IsDirty)
            {
                await RegisterDirtyInstanceInfo(inst);
            }

            try
            {
                EurekaHttpResponse<InstanceInfo> resp = await HttpClient.SendHeartBeatAsync(inst.AppName, inst.InstanceId, inst, InstanceStatus.UNKNOWN);
                _logger?.LogDebug("Renew {Application}/{Instance} returned: {StatusCode}", inst.AppName, inst.InstanceId, resp.StatusCode);
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    return await RegisterAsync();
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
                _logger?.LogError(e, "Renew Failed");
            }

            _logger?.LogDebug("Renew failed");
            return false;
        }

        protected internal async T.Task<Applications> FetchFullRegistryAsync()
        {
            long startingCounter = _registryFetchCounter;
            EurekaHttpResponse<Applications> resp = null;
            Applications fetched = null;

            if (string.IsNullOrEmpty(ClientConfig.RegistryRefreshSingleVipAddress))
            {
                resp = await HttpClient.GetApplicationsAsync();
            }
            else
            {
                resp = await HttpClient.GetVipAsync(ClientConfig.RegistryRefreshSingleVipAddress);
            }

            _logger?.LogDebug(
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
                _logger?.LogWarning("FetchFullRegistry discarding fetch, race condition");
            }

            _logger?.LogDebug("FetchFullRegistry failed");
            return null;
        }

        protected internal async T.Task<Applications> FetchRegistryDeltaAsync()
        {
            long startingCounter = _registryFetchCounter;
            Applications delta = null;

            EurekaHttpResponse<Applications> resp = await HttpClient.GetDeltaAsync();
            _logger?.LogDebug("FetchRegistryDelta returned: {StatusCode}", resp.StatusCode);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                delta = resp.Response;
            }

            if (delta == null)
            {
                // Log
                return await FetchFullRegistryAsync();
            }

            if (Interlocked.CompareExchange(ref _registryFetchCounter, (startingCounter + 1) % long.MaxValue, startingCounter) == startingCounter)
            {
                _localRegionApps.UpdateFromDelta(delta);
                string hashCode = _localRegionApps.ComputeHashCode();
                if (!hashCode.Equals(delta.AppsHashCode))
                {
                    _logger?.LogWarning($"FetchRegistryDelta discarding delta, hashcodes mismatch: {hashCode}!={delta.AppsHashCode}");
                    return await FetchFullRegistryAsync();
                }
                else
                {
                    _localRegionApps.AppsHashCode = delta.AppsHashCode;
                    LastGoodDeltaRegistryFetchTimestamp = DateTime.UtcNow.Ticks;
                    return _localRegionApps;
                }
            }

            _logger?.LogDebug("FetchRegistryDelta failed");
            return null;
        }

        protected internal void RefreshInstanceInfo()
        {
            InstanceInfo info = _appInfoManager.InstanceInfo;
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
                    _logger?.LogDebug("RefreshInstanceInfo called, returning {status}", status);
                }
                catch (Exception e)
                {
                    _logger?.LogError(
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
            var regResult = await RegisterAsync();
            _logger?.LogDebug("Register dirty InstanceInfo returned {status}", regResult);
            if (regResult)
            {
                inst.IsDirty = false;
            }

            return regResult;
        }

        protected void Initialize()
        {
            _localRegionApps = new Applications
            {
                ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances
            };

            if (!ClientConfig.ShouldRegisterWithEureka && !ClientConfig.ShouldFetchRegistry)
            {
                return;
            }

            if (ClientConfig.ShouldRegisterWithEureka && _appInfoManager.InstanceInfo != null)
            {
                var result = RegisterAsync();
                if (!result.Result)
                {
                    _logger?.LogInformation("Initial Registration failed.");
                }

                _logger?.LogInformation("Starting HeartBeat");
                var intervalInMilli = _appInfoManager.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
                _heartBeatTimer = StartTimer("HeartBeat", intervalInMilli, this.HeartBeatTaskAsync);
                if (ClientConfig.ShouldOnDemandUpdateStatusChange)
                {
                    _appInfoManager.StatusChangedEvent += Instance_StatusChangedEvent;
                }
            }

            if (ClientConfig.ShouldFetchRegistry)
            {
                var result = FetchRegistryAsync(true);
                result.Wait();
                var intervalInMilli = ClientConfig.RegistryFetchIntervalSeconds * 1000;
                _cacheRefreshTimer = StartTimer("Query", intervalInMilli, CacheRefreshTaskAsync);
            }
        }

        private bool IsHealthCheckHandlerEnabled()
        {
            var config = ClientConfig as EurekaClientConfig;
            if (config != null)
            {
                return config.HealthCheckEnabled && HealthCheckHandler != null;
            }

            return HealthCheckHandler != null;
        }

        private void UpdateInstanceRemoteStatus()
        {
            // Determine this instance's status for this app and set to UNKNOWN if not found
            InstanceInfo info = _appInfoManager?.InstanceInfo;

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

                LastRemoteInstanceStatus = InstanceStatus.UNKNOWN;
            }
        }

        private async void HeartBeatTaskAsync()
        {
            if (_shutdown > 0)
            {
                return;
            }

            var result = await RenewAsync();
            if (!result)
            {
                _logger?.LogError("HeartBeat failed");
            }
        }

        private async void CacheRefreshTaskAsync()
        {
            if (_shutdown > 0)
            {
                return;
            }

            bool result = await FetchRegistryAsync(false);
            if (!result)
            {
                _logger?.LogError("CacheRefresh failed");
            }
        }
    }
}
