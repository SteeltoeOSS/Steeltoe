// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Computes the Eureka <see cref="InstanceStatus" /> from ASP.NET health checks and all registered Steeltoe <see cref="IHealthContributor" />s.
/// </summary>
internal sealed class EurekaHealthCheckHandler : IHealthCheckHandler
{
    private static readonly List<HealthStatus> HealthStatusOrder =
    [
        HealthStatus.Up,
        HealthStatus.Warning,
        HealthStatus.Unknown,
        HealthStatus.OutOfService,
        HealthStatus.Down
    ];

    private readonly IHealthAggregator _healthAggregator;
    private readonly IOptionsMonitor<HealthCheckServiceOptions> _healthOptionsMonitor;
    private readonly IServiceProvider _serviceProvider;

    public EurekaHealthCheckHandler(IHealthAggregator healthAggregator, IOptionsMonitor<HealthCheckServiceOptions> healthOptionsMonitor,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(healthAggregator);
        ArgumentNullException.ThrowIfNull(healthOptionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _healthAggregator = healthAggregator;
        _healthOptionsMonitor = healthOptionsMonitor;
        _serviceProvider = serviceProvider;
    }

    public async Task<InstanceStatus> GetStatusAsync(bool hasFirstHeartbeatCompleted, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope serviceScope = _serviceProvider.CreateAsyncScope();

        List<IHealthContributor> healthContributors = serviceScope.ServiceProvider.GetServices<IHealthContributor>().ToList();
        ICollection<HealthCheckRegistration> registrations = _healthOptionsMonitor.CurrentValue.Registrations;

        if (!hasFirstHeartbeatCompleted)
        {
            // We're being called during preparation for the first heartbeat. EurekaServerHealthContributor will return UNKNOWN because its
            // dependent information hasn't been gathered yet. Skip it, to avoid the local Eureka instance temporarily being taken out of service.
            healthContributors.RemoveAll(contributor => contributor is EurekaServerHealthContributor);
        }

        HealthCheckResult result = await _healthAggregator.AggregateAsync(healthContributors, registrations, serviceScope.ServiceProvider, cancellationToken);

        return AggregateStatus(result);
    }

    private static InstanceStatus AggregateStatus(HealthCheckResult result)
    {
        var healthStatus = HealthStatus.Up;

        foreach (HealthCheckResult nextResult in result.Details.Values.OfType<HealthCheckResult>())
        {
            if (HealthStatusOrder.IndexOf(healthStatus) < HealthStatusOrder.IndexOf(nextResult.Status))
            {
                healthStatus = nextResult.Status;
            }
        }

        return MapToInstanceStatus(healthStatus);
    }

    private static InstanceStatus MapToInstanceStatus(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Up => InstanceStatus.Up,
            HealthStatus.Warning => InstanceStatus.Up,
            HealthStatus.OutOfService => InstanceStatus.OutOfService,
            HealthStatus.Down => InstanceStatus.Down,
            _ => InstanceStatus.Unknown
        };
    }
}
