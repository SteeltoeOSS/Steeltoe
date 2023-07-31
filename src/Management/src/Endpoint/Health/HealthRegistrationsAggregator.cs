// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using MicrosoftHealthCheckResult = Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult;
using MicrosoftHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;
using SteeltoeHealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using SteeltoeHealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthRegistrationsAggregator : DefaultHealthAggregator, IHealthRegistrationsAggregator
{
    public SteeltoeHealthCheckResult Aggregate(ICollection<IHealthContributor> contributors, ICollection<HealthCheckRegistration> healthCheckRegistrations,
        IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(contributors);
        ArgumentGuard.NotNull(healthCheckRegistrations);
        ArgumentGuard.NotNull(serviceProvider);

        // get results from DefaultHealthAggregator first
        SteeltoeHealthCheckResult aggregatorResult = Aggregate(contributors, cancellationToken);

        // if there aren't any Microsoft health checks, return now
        if (healthCheckRegistrations.Count == 0)
        {
            return aggregatorResult;
        }

        ConcurrentDictionary<string, SteeltoeHealthCheckResult> healthChecks =
            AggregateMicrosoftHealthChecks(contributors, healthCheckRegistrations, serviceProvider, cancellationToken);

        return AddChecksSetStatus(aggregatorResult, healthChecks);
    }

    private static ConcurrentDictionary<string, SteeltoeHealthCheckResult> AggregateMicrosoftHealthChecks(IEnumerable<IHealthContributor> contributors,
        IEnumerable<HealthCheckRegistration> healthCheckRegistrations, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var healthChecks = new ConcurrentDictionary<string, SteeltoeHealthCheckResult>();
        var keys = new ConcurrentBag<string>(contributors.Select(contributor => contributor.Id));

        // run all HealthCheckRegistration checks in parallel
        Parallel.ForEach(healthCheckRegistrations, registration =>
        {
            string contributorName = GetKey(keys, registration.Name);
            SteeltoeHealthCheckResult healthCheckResult;

            try
            {
                healthCheckResult = RunMicrosoftHealthCheckAsync(serviceProvider, registration, cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception)
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

            healthCheckResult.Details = new Dictionary<string, object>(result.Data)
            {
                { "status", status.ToSnakeCaseString(SnakeCaseStyle.AllCaps) },
                { "description", result.Description }
            };

            if (result.Exception != null && !string.IsNullOrEmpty(result.Exception.Message))
            {
                healthCheckResult.Details.Add("error", result.Exception.Message);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
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
}
