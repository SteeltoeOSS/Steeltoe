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

using Newtonsoft.Json;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Util;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaApplicationHealthContributor : IHealthContributor
    {
        private readonly EurekaDiscoveryClient _discoveryClient;

        public EurekaApplicationHealthContributor(string id, EurekaDiscoveryClient discoveryClient)
        {
            _discoveryClient = discoveryClient;
            Id = id;
        }

        public HealthCheckResult Health()
        {
            var serviceInstances = _discoveryClient.GetInstancesByVipAddress(Id, false);
            var hasHealthyInstances = serviceInstances.Any(x => x.Status == InstanceStatus.UP);
            var result = new HealthCheckResult();
            result.Status = hasHealthyInstances ? HealthStatus.UP : HealthStatus.DOWN;
            result.Details.Add("status", result.Status.ToString());
            if (result.Status != HealthStatus.UP)
            {
                object healthDetails;
                if (serviceInstances.Any())
                {
                    var healthDetailsJson = serviceInstances.First().Metadata["health"];
                    healthDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(healthDetailsJson, new DictionaryObjectConverter());
                }
                else
                {
                    healthDetails = "No running instances";
                }

                result.Details.Add("details", healthDetails);
            }

            return result;
        }

        public string Id { get; }
    }
}