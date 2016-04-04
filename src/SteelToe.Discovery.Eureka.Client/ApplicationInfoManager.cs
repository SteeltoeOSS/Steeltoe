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

using Microsoft.Extensions.Logging;
using SteelToe.Discovery.Eureka.AppInfo;
using System;

namespace SteelToe.Discovery.Eureka
{
    public class ApplicationInfoManager
    {
        private ApplicationInfoManager() { }
        private static readonly ApplicationInfoManager _instance = new ApplicationInfoManager();
        private ILogger _logger;
        private object _statusChangedLock = new object();

        public IEurekaInstanceConfig InstanceConfig { get; internal set; }
        public InstanceInfo InstanceInfo { get; internal set; }

        public event StatusChangedHandler StatusChangedEvent;

        public InstanceStatus InstanceStatus
        {
            get
            {
                if (InstanceInfo == null)
                    return InstanceStatus.UNKNOWN;
                return InstanceInfo.Status;
            }
            set
            {
                if (InstanceInfo == null)
                    return;
                lock (_statusChangedLock)
                {
                    InstanceStatus prev = InstanceInfo.Status;
                    if (prev != value)
                    {
                        InstanceInfo.Status = value;
                        if (StatusChangedEvent != null)
                        {
                            try
                            {
                                StatusChangedEvent(this, new StatusChangedArgs(prev, value, InstanceInfo.InstanceId));
                            }
                            catch (Exception e)
                            {
                                _logger?.LogError("StatusChangedEvent Exception:", e);
                            }
                        }

                    }
                }
            }
        }


        public static ApplicationInfoManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public void Initialize(IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
        {
            if (instanceConfig == null)
            {
                throw new ArgumentNullException(nameof(instanceConfig));
            }
            _logger = logFactory?.CreateLogger<ApplicationInfoManager>();
            InstanceConfig = instanceConfig;
            InstanceInfo = InstanceInfo.FromInstanceConfig(instanceConfig);
        }

        public void RefreshLeaseInfo()
        {
            if (InstanceInfo == null || InstanceConfig == null)
            {
                return;
            }

            if (InstanceInfo.LeaseInfo == null)
            {
                return;
            }

            if (InstanceInfo.LeaseInfo.DurationInSecs != InstanceConfig.LeaseExpirationDurationInSeconds ||
                InstanceInfo.LeaseInfo.RenewalIntervalInSecs != InstanceConfig.LeaseRenewalIntervalInSeconds)
            {
                LeaseInfo newLease = new LeaseInfo()
                {
                    DurationInSecs = InstanceConfig.LeaseExpirationDurationInSeconds,
                    RenewalIntervalInSecs = InstanceConfig.LeaseRenewalIntervalInSeconds

                };
                InstanceInfo.LeaseInfo = newLease;
                InstanceInfo.IsDirty = true;
            }
        }
    }
    public class StatusChangedArgs : EventArgs
    {
        public InstanceStatus Previous { get; private set; }
        public InstanceStatus Current { get; private set; }

        public string InstanceId { get; private set; }
        public StatusChangedArgs(InstanceStatus prev, InstanceStatus current, string instanceId)
        {
            Previous = prev;
            Current = current;
            InstanceId = instanceId;
        }
    }

    public delegate void StatusChangedHandler(object sender, StatusChangedArgs args);
}
