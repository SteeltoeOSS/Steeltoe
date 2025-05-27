// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.Extensions;
using MicrosoftHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using MicrosoftHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using SteeltoeHealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using SteeltoeHealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Common.HealthChecks;

internal sealed class HealthAggregator : IHealthAggregator
{
    public async Task<SteeltoeHealthCheckResult> AggregateAsync(ICollection<IHealthContributor> contributors,
        ICollection<HealthCheckRegistration> healthCheckRegistrations, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(healthCheckRegistrations);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        SteeltoeHealthCheckResult contributorsResult = await AggregateHealthContributorsAsync(contributors, cancellationToken);

        IDictionary<string, SteeltoeHealthCheckResult> healthCheckResults =
            await AggregateMicrosoftHealthChecksAsync(contributors, healthCheckRegistrations, serviceProvider, cancellationToken);

        return AddChecksSetStatus(contributorsResult, healthCheckResults);
    }

    private async Task<SteeltoeHealthCheckResult> AggregateHealthContributorsAsync(ICollection<IHealthContributor> contributors,
        CancellationToken cancellationToken)
    {
        if (contributors.Count == 0)
        {
            return new SteeltoeHealthCheckResult();
        }

        var aggregatorResult = new SteeltoeHealthCheckResult();
        var healthChecks = new ConcurrentDictionary<string, SteeltoeHealthCheckResult>();
        var keys = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(contributors, cancellationToken, async (contributor, _) =>
        {
            string contributorId = GetKey(keys, contributor.Id);
            SteeltoeHealthCheckResult? healthCheckResult;

            try
            {
                healthCheckResult = await contributor.CheckHealthAsync(cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                healthCheckResult = new SteeltoeHealthCheckResult();
            }

            if (healthCheckResult != null)
            {
                healthChecks.TryAdd(contributorId, healthCheckResult);
            }
        });

        return AddChecksSetStatus(aggregatorResult, healthChecks);
    }

    private static async Task<IDictionary<string, SteeltoeHealthCheckResult>> AggregateMicrosoftHealthChecksAsync(ICollection<IHealthContributor> contributors,
        ICollection<HealthCheckRegistration> healthCheckRegistrations, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        if (healthCheckRegistrations.Count == 0)
        {
            return new Dictionary<string, SteeltoeHealthCheckResult>();
        }

        var healthChecks = new ConcurrentDictionary<string, SteeltoeHealthCheckResult>();
        var keys = new ConcurrentBag<string>(contributors.Select(contributor => contributor.Id));

        // run all HealthCheckRegistration checks in parallel
        await Parallel.ForEachAsync(healthCheckRegistrations, cancellationToken, async (registration, _) =>
        {
            string contributorName = GetKey(keys, registration.Name);
            SteeltoeHealthCheckResult healthCheckResult;

            try
            {
                healthCheckResult = await RunMicrosoftHealthCheckAsync(serviceProvider, registration, cancellationToken);
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                healthCheckResult = new SteeltoeHealthCheckResult();
            }

            healthChecks.TryAdd(contributorName, healthCheckResult);
        });

        return healthChecks;
    }

    private static async Task<SteeltoeHealthCheckResult> RunMicrosoftHealthCheckAsync(IServiceProvider serviceProvider, HealthCheckRegistration registration,
        CancellationToken cancellationToken)
    {
        var context = new HealthCheckContext
        {
            Registration = registration
        };

        var healthCheckResult = new SteeltoeHealthCheckResult();

        try
        {
            IHealthCheck check = registration.Factory(serviceProvider);
            MicrosoftHealthCheckResult result = await check.CheckHealthAsync(context, cancellationToken);

            SteeltoeHealthStatus status = ToHealthStatus(result.Status);
            healthCheckResult.Status = status; // Only used for aggregate, doesn't get reported
            healthCheckResult.Description = result.Description;

            foreach ((string key, object value) in result.Data)
            {
                healthCheckResult.Details[key] = value;
            }

            healthCheckResult.Details["status"] = status.ToSnakeCaseString(SnakeCaseStyle.AllCaps);

            if (result.Description != null)
            {
                healthCheckResult.Details.Add("description", result.Description);
            }

            if (result.Exception != null && !string.IsNullOrEmpty(result.Exception.Message))
            {
                healthCheckResult.Details.Add("error", result.Exception.Message);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            // Catch all exceptions so that a status can always be returned
            healthCheckResult.Details.Add("exception", exception.Message);
        }

        return healthCheckResult;
    }

    private static SteeltoeHealthStatus ToHealthStatus(MicrosoftHealthStatus status)
    {
        return status switch
        {
            MicrosoftHealthStatus.Healthy => SteeltoeHealthStatus.Up,
            MicrosoftHealthStatus.Degraded => SteeltoeHealthStatus.Warning,
            MicrosoftHealthStatus.Unhealthy => SteeltoeHealthStatus.Down,
            _ => SteeltoeHealthStatus.Unknown
        };
    }

    private static string GetKey(ConcurrentBag<string> keys, string key)
    {
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

    private static SteeltoeHealthCheckResult AddChecksSetStatus(SteeltoeHealthCheckResult result, IDictionary<string, SteeltoeHealthCheckResult> healthChecks)
    {
        var orderedCheckResults = new SortedDictionary<CheckResultOrderingKey, SteeltoeHealthCheckResult>(CheckResultOrderingKeyComparer.Instance);

        foreach ((string name, SteeltoeHealthCheckResult checkResult) in healthChecks)
        {
            if (checkResult.Status > result.Status)
            {
                result.Status = checkResult.Status;
            }

            var orderingKey = new CheckResultOrderingKey(checkResult.Status, name);
            orderedCheckResults.Add(orderingKey, checkResult);
        }

        foreach ((CheckResultOrderingKey orderingKey, SteeltoeHealthCheckResult checkResult) in orderedCheckResults)
        {
            result.Details.Add(orderingKey.Name, checkResult);
        }

        return result;
    }

    private sealed record CheckResultOrderingKey(SteeltoeHealthStatus Status, string Name);

    private sealed class CheckResultOrderingKeyComparer : IComparer<CheckResultOrderingKey>
    {
        private static readonly List<SteeltoeHealthStatus> HealthStatusOrder =
        [
            SteeltoeHealthStatus.Down,
            SteeltoeHealthStatus.OutOfService,
            SteeltoeHealthStatus.Warning,
            SteeltoeHealthStatus.Unknown,
            SteeltoeHealthStatus.Up
        ];

        public static CheckResultOrderingKeyComparer Instance { get; } = new();

        private CheckResultOrderingKeyComparer()
        {
        }

        public int Compare(CheckResultOrderingKey? x, CheckResultOrderingKey? y)
        {
            if (x == null || y == null)
            {
                return Comparer<CheckResultOrderingKey>.Default.Compare(x, y);
            }

            int result = HealthStatusOrder.IndexOf(x.Status).CompareTo(HealthStatusOrder.IndexOf(y.Status));

            if (result == 0)
            {
                result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }

            return result;
        }
    }
}
