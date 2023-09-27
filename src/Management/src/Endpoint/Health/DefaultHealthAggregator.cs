// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health;

internal class DefaultHealthAggregator : IHealthAggregator
{
    public async Task<HealthCheckResult> AggregateAsync(ICollection<IHealthContributor> contributors, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(contributors);

        if (contributors.Count == 0)
        {
            return new HealthCheckResult();
        }

        var aggregatorResult = new HealthCheckResult();
        var healthChecks = new ConcurrentDictionary<string, HealthCheckResult>();
        var keyList = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(contributors, cancellationToken, async (contributor, _) =>
        {
            string contributorId = GetKey(keyList, contributor.Id);
            HealthCheckResult? healthCheckResult;

            try
            {
                healthCheckResult = await contributor.HealthAsync(cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                healthCheckResult = new HealthCheckResult();
            }

            if (healthCheckResult != null)
            {
                healthChecks.TryAdd(contributorId, healthCheckResult);
            }
        });

        return AddChecksSetStatus(aggregatorResult, healthChecks);
    }

    protected static string GetKey(ConcurrentBag<string> keys, string key)
    {
        ArgumentGuard.NotNull(keys);

        lock (keys)
        {
            // add the contributor with a -n appended to the id
            if (keys.Any(value => value == key))
            {
                string newKey = $"{key}-{keys.Count(value => value.StartsWith(key, StringComparison.Ordinal))}";
                keys.Add(newKey);
                return newKey;
            }

            keys.Add(key);
            return key;
        }
    }

    protected HealthCheckResult AddChecksSetStatus(HealthCheckResult result, ConcurrentDictionary<string, HealthCheckResult> healthChecks)
    {
        ArgumentGuard.NotNull(result);
        ArgumentGuard.NotNull(healthChecks);

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
