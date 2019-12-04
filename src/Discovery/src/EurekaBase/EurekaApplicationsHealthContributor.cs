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
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaApplicationsHealthContributor : IHealthContributor
    {
        private readonly EurekaDiscoveryClient _discoveryClient;
        private readonly ILogger<EurekaApplicationsHealthContributor> _logger;

        public EurekaApplicationsHealthContributor(EurekaDiscoveryClient discoveryClient, ILogger<EurekaApplicationsHealthContributor> logger = null)
        {
            _discoveryClient = discoveryClient;
            _logger = logger;
        }

        // Testing
        internal EurekaApplicationsHealthContributor()
        {
        }

        public HealthCheckResult Health()
        {
            var result = new HealthCheckResult();

            result.Status = HealthStatus.UP;
            result.Description = "No monitored applications";

            var appNames = GetMonitoredApplications(_discoveryClient.ClientConfig);

            foreach (var appName in appNames)
            {
                var app = _discoveryClient.GetApplication(appName);
                AddApplicationHealthStatus(appName, app, result);
            }

            if (result.Status != HealthStatus.UP)
            {
                result.Description = "At least one monitored application has no instances UP";
            }
            else
            {
                result.Description = "All monitored applications have at least one instance UP";
            }

            result.Details.Add("status", result.Status.ToString());
            result.Details.Add("statusDescription", result.Description);
            return result;
        }

        internal void AddApplicationHealthStatus(string appName, Application app, HealthCheckResult result)
        {
            if (app != null && app.Name == appName)
            {
                var upCount = app.Instances.Count(x => x.Status == InstanceStatus.UP);
                if (upCount <= 0)
                {
                    result.Status = HealthStatus.DOWN;
                }

                result.Details[appName] = upCount + " instances with UP status";
            }
            else
            {
                result.Status = HealthStatus.DOWN;
                result.Details[appName] = "No instances found";
            }
        }

        internal IList<string> GetMonitoredApplications(IEurekaClientConfig clientConfig)
        {
            IList<string> configApps = GetApplicationsFromConfig(clientConfig);
            if (configApps != null)
            {
                return configApps;
            }

            var regApps = _discoveryClient.Applications.GetRegisteredApplications();
            return regApps.Select((app) => app.Name).ToList();
        }

        internal IList<string> GetApplicationsFromConfig(IEurekaClientConfig clientConfig)
        {
            if (clientConfig is EurekaClientConfig config)
            {
                var monitoredApps = config.HealthMonitoredApps?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (monitoredApps != null && monitoredApps.Length > 0)
                {
                    List<string> results = new List<string>();
                    foreach (var str in monitoredApps)
                    {
                        results.Add(str.Trim());
                    }

                    return results;
                }
            }

            return null;
        }

        public string Id { get; } = "eurekaApplications";
    }
}