// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Computes the Eureka InstanceStatus from all of the Steeltoe Health contributors registered for the application.
/// When this handler is added to the container it registers with the DiscoveryClient as a IHealthCheckHandler.
/// The DiscoveryClient will then call it each time it is computing the InstanceStatus of the application.
/// </summary>
public class EurekaHealthCheckHandler : IHealthCheckHandler
{
    protected internal IList<IHealthContributor> _contributors;
    private readonly ILogger _logger;

    public EurekaHealthCheckHandler(ILogger logger = null)
    {
        _logger = logger;
    }

    public EurekaHealthCheckHandler(IEnumerable<IHealthContributor> contributors, ILogger<EurekaHealthCheckHandler> logger = null)
        : this(logger)
    {
        _contributors = contributors.ToList();
    }

    public virtual InstanceStatus GetStatus(InstanceStatus currentStatus)
    {
        var results = DoHealthChecks(_contributors);
        var status = AggregateStatus(results);
        return MapToInstanceStatus(status);
    }

    protected internal virtual List<HealthCheckResult> DoHealthChecks(IList<IHealthContributor> contributors)
    {
        var results = new List<HealthCheckResult>();
        foreach (var contributor in contributors)
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

    protected internal virtual HealthStatus AggregateStatus(List<HealthCheckResult> results)
    {
        var considered = new List<HealthStatus>();

        // Filter out warnings, ignored
        foreach (var result in results)
        {
            if (result.Status != HealthStatus.WARNING)
            {
                considered.Add(result.Status);
            }
        }

        // Nothing left
        if (considered.Count == 0)
        {
            return HealthStatus.UNKNOWN;
        }

        // Compute final
        var final = HealthStatus.UNKNOWN;
        foreach (var status in considered)
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
        if (status == HealthStatus.OUT_OF_SERVICE)
        {
            return InstanceStatus.OUT_OF_SERVICE;
        }

        if (status == HealthStatus.DOWN)
        {
            return InstanceStatus.DOWN;
        }

        if (status == HealthStatus.UP)
        {
            return InstanceStatus.UP;
        }

        return InstanceStatus.UNKNOWN;
    }
}
