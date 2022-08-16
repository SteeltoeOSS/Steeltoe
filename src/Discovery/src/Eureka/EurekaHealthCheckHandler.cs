// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Computes the Eureka InstanceStatus from all of the Steeltoe Health contributors registered for the application. When this handler is added to the
/// container it registers with the DiscoveryClient as a IHealthCheckHandler. The DiscoveryClient will then call it each time it is computing the
/// InstanceStatus of the application.
/// </summary>
public class EurekaHealthCheckHandler : IHealthCheckHandler
{
    private readonly ILogger _logger;
    protected internal IList<IHealthContributor> Contributors;

    public EurekaHealthCheckHandler(ILogger logger = null)
    {
        _logger = logger;
    }

    public EurekaHealthCheckHandler(IEnumerable<IHealthContributor> contributors, ILogger<EurekaHealthCheckHandler> logger = null)
        : this(logger)
    {
        Contributors = contributors.ToList();
    }

    public virtual InstanceStatus GetStatus(InstanceStatus currentStatus)
    {
        IList<HealthCheckResult> results = DoHealthChecks(Contributors);
        HealthStatus status = AggregateStatus(results);
        return MapToInstanceStatus(status);
    }

    protected internal virtual IList<HealthCheckResult> DoHealthChecks(IList<IHealthContributor> contributors)
    {
        var results = new List<HealthCheckResult>();

        foreach (IHealthContributor contributor in contributors)
        {
            try
            {
                results.Add(contributor.Health());
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Health Contributor {id} failed, status not included!", contributor.Id);
            }
        }

        return results;
    }

    protected internal virtual HealthStatus AggregateStatus(IEnumerable<HealthCheckResult> results)
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

    protected internal virtual InstanceStatus MapToInstanceStatus(HealthStatus status)
    {
        if (status == HealthStatus.OutOfService)
        {
            return InstanceStatus.OutOfService;
        }

        if (status == HealthStatus.Down)
        {
            return InstanceStatus.Down;
        }

        if (status == HealthStatus.Up)
        {
            return InstanceStatus.Up;
        }

        return InstanceStatus.Unknown;
    }
}
