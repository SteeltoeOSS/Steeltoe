// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal abstract partial class AvailabilityStateHealthContributor : IHealthContributor
{
    private readonly IDictionary<AvailabilityState, HealthStatus> _stateMappings;
    private readonly ILogger _logger;

    protected abstract bool IsEnabled { get; }

    public abstract string Id { get; }

    protected AvailabilityStateHealthContributor(IDictionary<AvailabilityState, HealthStatus> stateMappings, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(stateMappings);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _stateMappings = stateMappings;
        _logger = loggerFactory.CreateLogger<AvailabilityStateHealthContributor>();
    }

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        HealthCheckResult? result = IsEnabled ? Health() : null;
        return Task.FromResult(result);
    }

    private HealthCheckResult Health()
    {
        var health = new HealthCheckResult();
        AvailabilityState? currentHealth = GetState();

        if (currentHealth == null)
        {
            LogFailedToGetState();
            health.Description = "Failed to get current availability state";
        }
        else
        {
            if (_stateMappings.TryGetValue(currentHealth, out HealthStatus status))
            {
                health.Status = status;
            }
            else
            {
                LogFailedToMapState(currentHealth);
                health.Description = "Failed to map current availability state";
            }
        }

        return health;
    }

    protected abstract AvailabilityState? GetState();

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get current availability state.")]
    private partial void LogFailedToGetState();

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to map availability state {State}.")]
    private partial void LogFailedToMapState(AvailabilityState state);
}
