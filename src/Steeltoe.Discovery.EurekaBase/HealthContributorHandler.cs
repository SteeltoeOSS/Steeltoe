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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Discovery.Eureka
{
    public class HealthContributorHandler : IHealthCheckHandler
    {
        private EurekaApplicationInfoManager _appManager;
        private IList<IHealthContributor> _contributors;
        private Lazy<IEnumerable<IHealthContributor>> _contributorFactory;

        public HealthContributorHandler(EurekaApplicationInfoManager appManager, Lazy<IEnumerable<IHealthContributor>> contributors)
        {
            _appManager = appManager;
            _contributorFactory = contributors;
        }

        public InstanceStatus GetStatus(InstanceStatus currentStatus)
        {
            if (!_contributorFactory.IsValueCreated)
            {
                _contributors = _contributorFactory.Value.ToList();
            }

            if (!_contributors.Any())
            {
                return currentStatus;
            }

            var result = InstanceStatus.UP;
            var details = new Dictionary<string, object>();
            foreach (var contributor in _contributors)
            {
                InstanceStatus newStatus;
                try
                {
                    var health = contributor.Health();
                    details[contributor.Id] = health.Details;
                    switch (health.Status)
                    {
                        case HealthStatus.UP:
                            newStatus = InstanceStatus.UP;
                            break;
                        case HealthStatus.DOWN:
                            newStatus = InstanceStatus.DOWN;
                            break;
                        case HealthStatus.OUT_OF_SERVICE:
                            newStatus = InstanceStatus.OUT_OF_SERVICE;
                            break;
                        default:
                            newStatus = InstanceStatus.UNKNOWN;
                            break;
                    }
                }
                catch (Exception e)
                {
                    newStatus = InstanceStatus.DOWN;
                    details[contributor.Id] = new Dictionary<string, object> { { "error", e.ToString() } };
                }

                if (newStatus > result)
                {
                    result = newStatus;
                    _appManager.InstanceInfo.Metadata["health"] = JsonConvert.SerializeObject(details);
                }
            }

            return result;
        }
    }
}