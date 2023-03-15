// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health;

public class DefaultHealthAggregator : IHealthAggregator
{
    public HealthCheckResult Aggregate(IList<IHealthContributor> contributors)
    {
        if (contributors == null)
        {
            return new HealthCheckResult();
        }

        var aggregatorResult = new HealthCheckResult();
        var healthChecks = new ConcurrentDictionary<string, HealthCheckResult>();
        var keyList = new ConcurrentBag<string>();

        Parallel.ForEach(contributors, contributor =>
        {
            string contributorId = GetKey(keyList, contributor.Id);
            HealthCheckResult healthCheckResult = null;

            try
            {
                healthCheckResult = contributor.Health();
            }
            catch (Exception)
            {
                healthCheckResult = new HealthCheckResult();
            }

            healthChecks.TryAdd(contributorId, healthCheckResult);
        });

        return AddChecksSetStatus(aggregatorResult, healthChecks);
    }

    protected static string GetKey(ConcurrentBag<string> keys, string key)
    {
        lock (keys)
        {
            // add the contributor with a -n appended to the id
            if (keys.Any(k => k == key))
            {
                string newKey = $"{key}-{keys.Count(k => k.StartsWith(key, StringComparison.Ordinal))}";
                keys.Add(newKey);
                return newKey;
            }

            keys.Add(key);
            return key;
        }
    }

    protected HealthCheckResult AddChecksSetStatus(HealthCheckResult result, ConcurrentDictionary<string, HealthCheckResult> healthChecks)
    {
        foreach (KeyValuePair<string, HealthCheckResult> healthCheck in healthChecks)
        {
            if (healthCheck.Value.Status > result.Status)
            {
                result.Status = healthCheck.Value.Status;
            }

            result.Details.Add(healthCheck.Key, healthCheck.Value);
        }

        return result;
    }
}
