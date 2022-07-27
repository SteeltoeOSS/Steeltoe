// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Availability;

public abstract class AvailabilityHealthContributor : IHealthContributor
{
    public virtual string Id => throw new NotImplementedException();

    private readonly Dictionary<IAvailabilityState, HealthStatus> _stateMappings;
    private readonly ILogger _logger;

    protected AvailabilityHealthContributor(Dictionary<IAvailabilityState, HealthStatus> stateMappings, ILogger logger = null)
    {
        if (stateMappings is null)
        {
            throw new ArgumentNullException(nameof(stateMappings));
        }

        _stateMappings = stateMappings;
        _logger = logger;
    }

    public HealthCheckResult Health()
    {
        var health = new HealthCheckResult();
        var currentHealth = GetState();

        if (currentHealth == null)
        {
            _logger?.LogCritical("Failed to get current availability state");
            health.Description = "Failed to get current availability state";
        }
        else
        {
            try
            {
                health.Status = _stateMappings[currentHealth];
                health.Details.Add(currentHealth.GetType().Name, currentHealth.ToString());
            }
            catch (Exception e)
            {
                _logger?.LogCritical(e, "Failed to map current availability state");
                health.Description = "Failed to map current availability state";
            }
        }

        return health;
    }

    protected virtual IAvailabilityState GetState() => throw new NotImplementedException();
}