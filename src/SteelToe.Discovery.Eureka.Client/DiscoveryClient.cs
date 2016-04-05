//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Threading;

using SteelToe.Discovery.Eureka.AppInfo;
using SteelToe.Discovery.Eureka.Task;
using SteelToe.Discovery.Eureka.Transport;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SteelToe.Discovery.Eureka
{
    public class DiscoveryClient : IEurekaClient
    {
        private Timer _heartBeatTimer;
        private Timer _cacheRefreshTimer;
        private volatile Applications _localRegionApps;
        private long _registryFetchCounter = 0;
        private IEurekaHttpClient _httpClient;
        private Random _random = new Random();
        private ILogger _logger;
        private int _shutdown = 0;

        public long LastGoodHeartbeatTimestamp { get; internal set; }
        public long LastGoodFullRegistryFetchTimestamp { get; internal set; }
        public long LastGoodDeltaRegistryFetchTimestamp { get; internal set; }
        public long LastGoodRegistryFetchTimestamp { get; internal set; }
        public long LastGoodRegisterTimestamp { get; internal set; }
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

        public IEurekaClientConfig ClientConfig { get; internal set; }

        public IHealthCheckHandler HealthCheckHandler { get; set; }

        public DiscoveryClient(IEurekaClientConfig clientConfig, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null)
        {
            if (clientConfig == null)
            {
                throw new ArgumentNullException(nameof(clientConfig));
            }

            _logger = logFactory?.CreateLogger<DiscoveryClient>();
            ClientConfig = clientConfig;
            _localRegionApps = new Applications();
            _localRegionApps.ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances;
            _httpClient = httpClient;

            if (_httpClient == null)
            {
                _httpClient = new EurekaHttpClient(clientConfig, logFactory);
            }

            if (!ClientConfig.ShouldRegisterWithEureka && !ClientConfig.ShouldFetchRegistry)
            {
                return;
            }

            if (ClientConfig.ShouldRegisterWithEureka)
            {
                var result = RegisterAsync();
                result.Wait();

                var intervalInMilli = ApplicationInfoManager.Instance.InstanceInfo.LeaseInfo.RenewalIntervalInSecs * 1000;
                _heartBeatTimer = StartTimer("HeartBeat", intervalInMilli, this.HeartBeatTaskAsync);
                if (ClientConfig.ShouldOnDemandUpdateStatusChange)
                {
                    ApplicationInfoManager.Instance.StatusChangedEvent += Instance_StatusChangedEvent;
                }

            }

            if (ClientConfig.ShouldFetchRegistry)
            {
                var result = FetchRegistryAsync(true);
                result.Wait();

                var intervalInMilli = ClientConfig.RegistryFetchIntervalSeconds * 1000;
                _cacheRefreshTimer = StartTimer("Query", intervalInMilli, this.CacheRefreshTaskAsync);
            }
        }

        public Application GetApplication(string appName)
        {
            if (string.IsNullOrEmpty(appName))
            {
                throw new ArgumentException(nameof(appName));
            }

            var apps = Applications;
            if (apps != null)
                return apps.GetRegisteredApplication(appName);

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
                return null;
            var index = _random.Next() % results.Count;
            return results[index];

        }
        public async void ShutdownAsyc()
        {
     
            int shutdown = Interlocked.Exchange(ref _shutdown, 1);
            if (shutdown > 0)
                return;

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
                ApplicationInfoManager.Instance.StatusChangedEvent -= Instance_StatusChangedEvent;
            }

            if (ClientConfig.ShouldRegisterWithEureka)
            {
                InstanceInfo info = ApplicationInfoManager.Instance.InstanceInfo;
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

        internal protected async Task<bool> FetchRegistryAsync(bool fullUpdate)
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
                _logger?.LogError("FetchRegistry failed, Exception:", e);
                return false;
            }

            if (fetched != null)
            {
                _localRegionApps = fetched;
                _localRegionApps.ReturnUpInstancesOnly = ClientConfig.ShouldFilterOnlyUpInstances;
                LastGoodRegistryFetchTimestamp = DateTime.UtcNow.Ticks;

                // Notify about cache refresh before updating the instance remote status
                //onCacheRefreshed();

                //// Update remote status based on refreshed data held in the cache
                //updateInstanceRemoteStatus();
                _logger?.LogDebug("FetchRegistry succeeded");
                return true;
            }

            _logger?.LogDebug("FetchRegistry failed");
            return false;
        }
        internal protected async Task<bool> UnregisterAsync()
        {
            InstanceInfo inst = ApplicationInfoManager.Instance.InstanceInfo;
            if (inst == null)
                return false;
            try
            {
                EurekaHttpResponse resp = await HttpClient.CancelAsync(inst.AppName, inst.InstanceId);
                _logger?.LogDebug("Unregister {0}/{1} returned: {2}", inst.AppName, inst.InstanceId, resp.StatusCode);
                return resp.StatusCode == HttpStatusCode.OK;

            }
            catch (Exception e)
            {
                _logger?.LogError("Unregister failed, Exception:", e);
            }

            _logger?.LogDebug("Unregister failed");
            return false;

        }
        internal protected async Task<bool> RegisterAsync()
        {
            InstanceInfo inst = ApplicationInfoManager.Instance.InstanceInfo;
            if (inst == null)
                return false;
            try
            {
                EurekaHttpResponse resp = await HttpClient.RegisterAsync(inst);
                var result = resp.StatusCode == HttpStatusCode.NoContent;
                _logger?.LogDebug("Register {0}/{1} returned: {2}", inst.AppName, inst.InstanceId, resp.StatusCode);
                if (result)
                {
                    LastGoodRegisterTimestamp = System.DateTime.UtcNow.Ticks;
                }
                return result;

            }
            catch (Exception e)
            {
                _logger?.LogError("Register failed, Exception:", e);
            }

            _logger?.LogDebug("Register failed");
            return false;

        }

        internal protected async Task<bool> RenewAsync()
        {
            InstanceInfo inst = ApplicationInfoManager.Instance.InstanceInfo;
            if (inst == null)
                return false;

            try
            {
                EurekaHttpResponse<InstanceInfo> resp = await HttpClient.SendHeartBeatAsync(inst.AppName, inst.InstanceId, inst, InstanceStatus.UNKNOWN);
                _logger?.LogDebug("Renew {0}/{1} returned: {2}", inst.AppName, inst.InstanceId, resp.StatusCode);
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
                _logger?.LogError("Renew failed, Exception:", e);
            }

            _logger?.LogDebug("Renew failed");
            return false;
        }

        internal protected async Task<Applications> FetchFullRegistryAsync()
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

            _logger?.LogDebug("FetchFullRegistry returned: {0}, {1}", resp.StatusCode, ((resp.Response != null) ? resp.Response.ToString() : "null"));
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                fetched = resp.Response;
            }

            if (fetched != null && Interlocked.CompareExchange(ref _registryFetchCounter,
                ((startingCounter + 1) % long.MaxValue), startingCounter) == startingCounter)
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

        internal protected async Task<Applications> FetchRegistryDeltaAsync()
        {
            long startingCounter = _registryFetchCounter;
            Applications delta = null;

            EurekaHttpResponse<Applications> resp = await HttpClient.GetDeltaAsync();
            _logger?.LogDebug("FetchRegistryDelta returned: {0}", resp.StatusCode);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                delta = resp.Response;
            }

            if (delta == null)
            {
                // Log
                return await FetchFullRegistryAsync();
            }

            if (Interlocked.CompareExchange(ref _registryFetchCounter, ((startingCounter + 1) % long.MaxValue), startingCounter) == startingCounter)
            {
                _localRegionApps.UpdateFromDelta(delta);
                string hashCode = _localRegionApps.ComputeHashCode();
                if (!hashCode.Equals(delta.AppsHashCode))
                {
                    _logger?.LogWarning("FetchRegistryDelta discarding delta, hashcodes mismatch: {0}!={1}", hashCode, delta.AppsHashCode);
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
        internal protected void RefreshInstanceInfo()
        {
            InstanceInfo info = ApplicationInfoManager.Instance.InstanceInfo;
            if (info == null)
            {
                return;
            }

            ApplicationInfoManager.Instance.RefreshLeaseInfo();

            InstanceStatus status = InstanceStatus.UNKNOWN;
            if (HealthCheckHandler != null)
            {
                try
                {
                    status = HealthCheckHandler.GetStatus(info.Status);
                }
                catch (Exception e)
                {
                    _logger?.LogError("RefreshInstanceInfo HealthCheck handler exception: {0}, App: {1}, Instance: {2} marked DOWN",
                        e, info.AppName, info.InstanceId);
                    status = InstanceStatus.DOWN;
                }
            }

            if (status != InstanceStatus.UNKNOWN)
            {
                info.Status = status;
            }
        }

        internal async void Instance_StatusChangedEvent(object sender, StatusChangedArgs args)
        {

            // Log StatusChangedArgs
            InstanceInfo info = ApplicationInfoManager.Instance.InstanceInfo;
            if (info != null)
            {
                RefreshInstanceInfo();
                if (info.IsDirty)
                {
                    try
                    {
                        var result = await RegisterAsync();
                        if (result)
                        {
                            info.IsDirty = false;
                            //Log
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError("Instance_StatusChangedEvent Exception:", e);
                    }
                }
            }
        }

        internal protected Timer StartTimer(string name, int interval, Action task)
        {
            var timedTask = new TimedTask(name, task);
            var timer = new Timer(timedTask.Run, null, interval, interval);
            return timer;
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
