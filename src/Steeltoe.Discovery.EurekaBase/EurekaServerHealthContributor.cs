// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaServerHealthContributor : IHealthContributor
    {
        private readonly EurekaDiscoveryClient _discoveryClient;
        private readonly EurekaApplicationInfoManager _appInfoManager;
        private readonly ILogger<EurekaServerHealthContributor> _logger;

        public EurekaServerHealthContributor(EurekaDiscoveryClient discoveryClient, EurekaApplicationInfoManager appInfoManager, ILogger<EurekaServerHealthContributor> logger = null)
        {
            _discoveryClient = discoveryClient;
            _appInfoManager = appInfoManager;
            _logger = logger;
        }

        // Testing
        internal EurekaServerHealthContributor()
        {
        }

        public HealthCheckResult Health()
        {
            var result = new HealthCheckResult();
            AddHealthStatus(result);
            AddApplications(_discoveryClient.Applications, result);
            return result;
        }

        internal void AddHealthStatus(HealthCheckResult result)
        {
            AddRemoteInstanceStatus(_discoveryClient.LastRemoteInstanceStatus, result);
            AddFetchStatus(_discoveryClient.ClientConfig, result, _discoveryClient.LastGoodRegistryFetchTimestamp);
            AddHeartbeatStatus(_discoveryClient.ClientConfig, _appInfoManager.InstanceConfig, result, _discoveryClient.LastGoodHeartbeatTimestamp);
        }

        internal void AddRemoteInstanceStatus(InstanceStatus lastRemoteInstanceStatus, HealthCheckResult result)
        {
            result.Status = MakeHealthStatus(_discoveryClient.LastRemoteInstanceStatus);
            result.Details.Add("status", result.Status.ToString());
            result.Description = "Last remote instance status from Eureka server";
            result.Details.Add("statusDescription", "Last remote instance status from Eureka server");
        }

        internal void AddHeartbeatStatus(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, HealthCheckResult result, long lastGoodHeartbeatTimeTicks)
        {
            if (clientConfig != null && clientConfig.ShouldRegisterWithEureka)
            {
                var lastGoodHeartbeatPeriod = GetLastGoodHeartbeatTimePeriod(lastGoodHeartbeatTimeTicks);
                if (lastGoodHeartbeatPeriod <= 0)
                {
                    result.Details.Add("heartbeat", "Eureka discovery client has not yet successfully connected to a Eureka server");
                }
                else if (lastGoodHeartbeatPeriod > ((instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond) * 2))
                {
                    result.Details.Add("heartbeat", "Eureka discovery client is reporting failures connecting to a Eureka server");
                    result.Details.Add("heartbeatFailures", lastGoodHeartbeatPeriod / (instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond));
                }

                if (lastGoodHeartbeatTimeTicks > 0)
                {
                    result.Details.Add("heartbeatTime", new DateTime(lastGoodHeartbeatTimeTicks).ToString("s"));
                }
                else
                {
                    result.Details.Add("heartbeatTime", "Unknown");
                }

                result.Details.Add("heartbeatStatus", HealthStatus.UP.ToString());
                return;
            }

            result.Details.Add("heartbeatStatus", HealthStatus.UNKNOWN.ToString());
        }

        internal void AddFetchStatus(IEurekaClientConfig clientConfig, HealthCheckResult result, long lastGoodFetchTimeTicks)
        {
            if (clientConfig != null && clientConfig.ShouldFetchRegistry)
            {
                var lastGoodFetchPeriod = GetLastGoodRegistryFetchTimePeriod(lastGoodFetchTimeTicks);
                if (lastGoodFetchPeriod <= 0)
                {
                    result.Details.Add("fetch", "Eureka discovery client has not yet successfully connected to a Eureka server");
                }
                else if (lastGoodFetchPeriod > ((clientConfig.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond) * 2))
                {
                    result.Details.Add("fetch", "Eureka discovery client is reporting failures connecting to a Eureka server");
                    result.Details.Add("fetchFailures", lastGoodFetchPeriod / (clientConfig.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond));
                }

                if (lastGoodFetchTimeTicks > 0)
                {
                    result.Details.Add("fetchTime", new DateTime(lastGoodFetchTimeTicks).ToString("s"));
                }
                else
                {
                    result.Details.Add("fetchTime", "Unknown");
                }

                result.Details.Add("fetchStatus", HealthStatus.UP.ToString());
                return;
            }

            result.Details.Add("fetchStatus", HealthStatus.UNKNOWN.ToString());
        }

        internal HealthStatus MakeHealthStatus(InstanceStatus lastRemoteInstanceStatus)
        {
            if (lastRemoteInstanceStatus == InstanceStatus.DOWN)
            {
                return HealthStatus.DOWN;
            }

            if (lastRemoteInstanceStatus == InstanceStatus.OUT_OF_SERVICE)
            {
                return HealthStatus.OUT_OF_SERVICE;
            }

            if (lastRemoteInstanceStatus == InstanceStatus.UP)
            {
                return HealthStatus.UP;
            }

            return HealthStatus.UNKNOWN;
        }

        internal void AddApplications(Applications applications, HealthCheckResult result)
        {
            Dictionary<string, int> apps = new Dictionary<string, int>();

            if (applications != null)
            {
                var registered = applications.GetRegisteredApplications();
                foreach (var app in registered)
                {
                    if (app.Instances?.Count > 0)
                    {
                        apps.Add(app.Name, app.Instances.Count);
                    }
                }
            }

            if (apps.Count > 0)
            {
                result.Details.Add("applications", apps);
            }
            else
            {
                result.Details.Add("applications", "NONE");
            }
        }

        private long GetLastGoodRegistryFetchTimePeriod(long lastGoodRegistryFetchTimestamp)
        {
            return lastGoodRegistryFetchTimestamp <= 0L ? lastGoodRegistryFetchTimestamp : DateTime.UtcNow.Ticks - lastGoodRegistryFetchTimestamp;
        }

        private long GetLastGoodHeartbeatTimePeriod(long lastGoodHeartbeatTimestamp)
        {
            return lastGoodHeartbeatTimestamp <= 0L ? lastGoodHeartbeatTimestamp : DateTime.UtcNow.Ticks - lastGoodHeartbeatTimestamp;
        }

        public string Id => "eurekaServer";
    }
}