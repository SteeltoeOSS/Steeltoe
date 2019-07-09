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

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthRegistrationsAggregator : DefaultHealthAggregator, IHealthRegistrationsAggregator
    {
        public HealthCheckResult Aggregate(IList<IHealthContributor> contributors, IOptionsMonitor<HealthCheckServiceOptions> healthServiceOptions, IServiceProvider serviceProvider)
        {
            var result = Aggregate(contributors);

            if (healthServiceOptions == null)
            {
                return result;
            }

            var contributorIds = contributors.Select(x => x.Id);
            foreach (var registration in healthServiceOptions.CurrentValue.Registrations)
            {
                HealthCheckResult h = registration.HealthCheck(serviceProvider).GetAwaiter().GetResult();

                if (h.Status > result.Status)
                {
                    result.Status = h.Status;
                }

                var key = GetKey(result, registration.Name);
                result.Details.Add(key, h);
                var possibleDuplicate = contributorIds.FirstOrDefault(id => id.IndexOf(registration.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(possibleDuplicate))
                {
                    var logger = serviceProvider.GetService(typeof(ILogger<HealthRegistrationsAggregator>)) as ILogger;
                    logger?.LogDebug($"Possible duplicate HealthCheck registation {registration.Name}, {possibleDuplicate} ");
                }
            }

            return result;
        }
    }
}
