// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Common.Availability;

public abstract class AvailabilityHealthContributor : IHealthContributor
{
    private readonly Dictionary<IAvailabilityState, HealthStatus> _stateMappings;
    private readonly ILogger _logger;

    public virtual string Id => throw new NotImplementedException();

    protected AvailabilityHealthContributor(Dictionary<IAvailabilityState, HealthStatus> stateMappings, ILogger logger = null)
    {
        ArgumentGuard.NotNull(stateMappings);

        _stateMappings = stateMappings;
        _logger = logger;
    }

    public HealthCheckResult Health()
    {
        var health = new HealthCheckResult();
        IAvailabilityState currentHealth = GetState();

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

    protected virtual IAvailabilityState GetState()
    {
        throw new NotImplementedException();
    }
}
