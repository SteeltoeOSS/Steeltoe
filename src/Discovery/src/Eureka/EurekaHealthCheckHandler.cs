// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Computes the Eureka <see cref="InstanceStatus" /> from all Steeltoe <see cref="IHealthContributor" />s registered for the application. When this
/// handler is added to the container, it registers with the discovery client as a <see cref="IHealthCheckHandler" />. The discovery client will then
/// call it each time it is computing the instance status of the application.
/// </summary>
public sealed class EurekaHealthCheckHandler : IHealthCheckHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public EurekaHealthCheckHandler(IServiceProvider serviceProvider, ILogger<EurekaHealthCheckHandler> logger)
    {
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(logger);

        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<InstanceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope serviceScope = _serviceProvider.CreateAsyncScope();
        IHealthContributor[] healthContributors = serviceScope.ServiceProvider.GetRequiredService<IEnumerable<IHealthContributor>>().ToArray();

        List<HealthCheckResult> results = await DoHealthChecksAsync(healthContributors, cancellationToken);
        HealthStatus status = AggregateStatus(results);
        return MapToInstanceStatus(status);
    }

    private async Task<List<HealthCheckResult>> DoHealthChecksAsync(IEnumerable<IHealthContributor> healthContributors, CancellationToken cancellationToken)
    {
        var results = new List<HealthCheckResult>();

        foreach (IHealthContributor contributor in healthContributors)
        {
            try
            {
                HealthCheckResult? result = await contributor.CheckHealthAsync(cancellationToken);

                if (result != null)
                {
                    results.Add(result);
                }
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                _logger?.LogError(exception, "Health Contributor {id} failed, status not included!", contributor.Id);
            }
        }

        return results;
    }

    private static HealthStatus AggregateStatus(List<HealthCheckResult> results)
    {
        var considered = new List<HealthStatus>();

        // Filter out warnings, ignored
        foreach (HealthCheckResult result in results)
        {
            if (result.Status != HealthStatus.Warning)
            {
                considered.Add(result.Status);
            }
        }

        // Nothing left
        if (considered.Count == 0)
        {
            return HealthStatus.Unknown;
        }

        // Compute final
        var final = HealthStatus.Unknown;

        foreach (HealthStatus status in considered)
        {
            if (status > final)
            {
                final = status;
            }
        }

        return final;
    }

    private static InstanceStatus MapToInstanceStatus(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.OutOfService => InstanceStatus.OutOfService,
            HealthStatus.Down => InstanceStatus.Down,
            HealthStatus.Up => InstanceStatus.Up,
            _ => InstanceStatus.Unknown
        };
    }
}
