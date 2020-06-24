// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthRegistrationsAggregator : DefaultHealthAggregator, IHealthRegistrationsAggregator
    {
        public HealthCheckResult Aggregate(IList<IHealthContributor> contributors, ICollection<HealthCheckRegistration> healthCheckRegistrations, IServiceProvider serviceProvider)
        {
            var result = Aggregate(contributors);

            if (healthCheckRegistrations == null)
            {
                return result;
            }

            var contributorIds = contributors.Select(x => x.Id);
            foreach (var registration in healthCheckRegistrations)
            {
                var h = registration.HealthCheck(serviceProvider).GetAwaiter().GetResult();

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
