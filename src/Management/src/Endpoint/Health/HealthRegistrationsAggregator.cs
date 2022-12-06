// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.HealthChecks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthRegistrationsAggregator : DefaultHealthAggregator, IHealthRegistrationsAggregator
{
    public HealthCheckResult Aggregate(IList<IHealthContributor> contributors, ICollection<HealthCheckRegistration> healthCheckRegistrations,
        IServiceProvider serviceProvider)
    {
        // get results from DefaultHealthAggregator first
        HealthCheckResult aggregatorResult = Aggregate(contributors);

        // if there aren't any MSFT interfaced health checks, return now
        if (healthCheckRegistrations == null)
        {
            return aggregatorResult;
        }

        var healthChecks = new ConcurrentDictionary<string, HealthCheckResult>();
        var keyList = new ConcurrentBag<string>(contributors.Select(x => x.Id));

        // run all HealthCheckRegistration checks in parallel
        Parallel.ForEach(healthCheckRegistrations, registration =>
        {
            string contributorName = GetKey(keyList, registration.Name);
            HealthCheckResult healthCheckResult = null;

            try
            {
                healthCheckResult = registration.HealthCheckAsync(serviceProvider).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                healthCheckResult = new HealthCheckResult();
            }

            healthChecks.TryAdd(contributorName, healthCheckResult);
        });

        return AddChecksSetStatus(aggregatorResult, healthChecks);
    }
}
