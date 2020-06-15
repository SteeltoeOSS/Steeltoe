// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
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
            var remoteStatus = AddRemoteInstanceStatus(_discoveryClient.LastRemoteInstanceStatus, result);
            var fetchStatus = AddFetchStatus(_discoveryClient.ClientConfig, result, _discoveryClient.LastGoodRegistryFetchTimestamp);
            var heartBeatStatus = AddHeartbeatStatus(_discoveryClient.ClientConfig, _appInfoManager.InstanceConfig, result, _discoveryClient.LastGoodHeartbeatTimestamp);

            result.Status = remoteStatus;
            if (fetchStatus > result.Status)
            {
                result.Status = fetchStatus;
            }

            if (heartBeatStatus > result.Status)
            {
                result.Status = heartBeatStatus;
            }

            result.Details.Add("status", result.Status.ToString());
        }

        internal HealthStatus AddRemoteInstanceStatus(InstanceStatus lastRemoteInstanceStatus, HealthCheckResult result)
        {
            var remoteStatus = MakeHealthStatus(_discoveryClient.LastRemoteInstanceStatus);
            result.Details.Add("remoteInstStatus", remoteStatus.ToString());
            return remoteStatus;
        }

        internal HealthStatus AddHeartbeatStatus(IEurekaClientConfig clientConfig, IEurekaInstanceConfig instanceConfig, HealthCheckResult result, long lastGoodHeartbeatTimeTicks)
        {
            if (clientConfig != null && clientConfig.ShouldRegisterWithEureka)
            {
                var lastGoodHeartbeatPeriod = GetLastGoodHeartbeatTimePeriod(lastGoodHeartbeatTimeTicks);
                if (lastGoodHeartbeatPeriod <= 0)
                {
                    result.Details.Add("heartbeat", "Not yet successfully connected");
                    result.Details.Add("heartbeatStatus", HealthStatus.UNKNOWN.ToString());
                    result.Details.Add("heartbeatTime", "UNKNOWN");
                    return HealthStatus.UNKNOWN;
                }
                else if (lastGoodHeartbeatPeriod > ((instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond) * 2))
                {
                    result.Details.Add("heartbeat", "Reporting failures connecting");
                    result.Details.Add("heartbeatStatus", HealthStatus.DOWN.ToString());
                    result.Details.Add("heartbeatTime", new DateTime(lastGoodHeartbeatTimeTicks).ToString("s"));
                    result.Details.Add("heartbeatFailures", lastGoodHeartbeatPeriod / (instanceConfig.LeaseRenewalIntervalInSeconds * TimeSpan.TicksPerSecond));
                    return HealthStatus.DOWN;
                }

                result.Details.Add("heartbeat", "Successful");
                result.Details.Add("heartbeatStatus", HealthStatus.UP.ToString());
                result.Details.Add("heartbeatTime", new DateTime(lastGoodHeartbeatTimeTicks).ToString("s"));
                return HealthStatus.UP;
            }

            result.Details.Add("heartbeatStatus", "Not registering");
            return HealthStatus.UNKNOWN;
        }

        internal HealthStatus AddFetchStatus(IEurekaClientConfig clientConfig, HealthCheckResult result, long lastGoodFetchTimeTicks)
        {
            if (clientConfig != null && clientConfig.ShouldFetchRegistry)
            {
                var lastGoodFetchPeriod = GetLastGoodRegistryFetchTimePeriod(lastGoodFetchTimeTicks);
                if (lastGoodFetchPeriod <= 0)
                {
                    result.Details.Add("fetch", "Not yet successfully connected");
                    result.Details.Add("fetchStatus", HealthStatus.UNKNOWN.ToString());
                    result.Details.Add("fetchTime", "UNKNOWN");
                    return HealthStatus.UNKNOWN;
                }
                else if (lastGoodFetchPeriod > ((clientConfig.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond) * 2))
                {
                    result.Details.Add("fetch", "Reporting failures connecting");
                    result.Details.Add("fetchStatus", HealthStatus.DOWN.ToString());
                    result.Details.Add("fetchTime", new DateTime(lastGoodFetchTimeTicks).ToString("s"));
                    result.Details.Add("fetchFailures", lastGoodFetchPeriod / (clientConfig.RegistryFetchIntervalSeconds * TimeSpan.TicksPerSecond));
                    return HealthStatus.DOWN;
                }

                result.Details.Add("fetch", "Successful");
                result.Details.Add("fetchStatus", HealthStatus.UP.ToString());
                result.Details.Add("fetchTime", new DateTime(lastGoodFetchTimeTicks).ToString("s"));
                return HealthStatus.UP;
            }

            result.Details.Add("fetchStatus", "Not fetching");
            return HealthStatus.UNKNOWN;
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