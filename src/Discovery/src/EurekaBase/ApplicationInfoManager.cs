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
using System;

namespace Steeltoe.Discovery.Eureka
{
    public class ApplicationInfoManager
    {
        protected ApplicationInfoManager()
        {
        }

        protected static ApplicationInfoManager _instance = new ApplicationInfoManager();
        protected ILogger _logger;

        private object _statusChangedLock = new object();

        public static ApplicationInfoManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public virtual IEurekaInstanceConfig InstanceConfig { get; protected internal set; }

        public virtual InstanceInfo InstanceInfo { get; protected internal set; }

        public virtual event StatusChangedHandler StatusChangedEvent;

        public virtual InstanceStatus InstanceStatus
        {
            get
            {
                if (InstanceInfo == null)
                {
                    return InstanceStatus.UNKNOWN;
                }

                return InstanceInfo.Status;
            }

            set
            {
                if (InstanceInfo == null)
                {
                    return;
                }

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

        public virtual void Initialize(IEurekaInstanceConfig instanceConfig, ILoggerFactory logFactory = null)
        {
            _logger = logFactory?.CreateLogger<ApplicationInfoManager>();
            InstanceConfig = instanceConfig ?? throw new ArgumentNullException(nameof(instanceConfig));
            InstanceInfo = InstanceInfo.FromInstanceConfig(instanceConfig);
        }

        public virtual void RefreshLeaseInfo()
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

    public delegate void StatusChangedHandler(object sender, StatusChangedArgs args);
}
