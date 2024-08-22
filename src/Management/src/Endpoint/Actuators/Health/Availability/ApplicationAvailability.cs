// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Availability;

public sealed class ApplicationAvailability
{
    public const string LivenessKey = "Liveness";
    public const string ReadinessKey = "Readiness";

    private readonly Dictionary<string, AvailabilityState> _availabilityStates = [];
    private readonly ILogger<ApplicationAvailability> _logger;

    public event EventHandler? LivenessChanged;
    public event EventHandler? ReadinessChanged;

    public ApplicationAvailability(ILogger<ApplicationAvailability> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public AvailabilityState? GetLivenessState()
    {
        return GetAvailabilityState(LivenessKey);
    }

    public AvailabilityState? GetReadinessState()
    {
        return GetAvailabilityState(ReadinessKey);
    }

    public AvailabilityState? GetAvailabilityState(string availabilityType)
    {
        return _availabilityStates.GetValueOrDefault(availabilityType);
    }

    /// <summary>
    /// Set the availability state for a given availability type.
    /// </summary>
    /// <param name="availabilityType">
    /// String name for the type of state to set. See <see cref="LivenessKey" /> or <see cref="ReadinessKey" /> for convenience.
    /// </param>
    /// <param name="newState">
    /// The new <see cref="AvailabilityState" />.
    /// </param>
    /// <param name="caller">
    /// Logged at trace level for tracking origin of state change.
    /// </param>
    public void SetAvailabilityState(string availabilityType, AvailabilityState newState, string? caller)
    {
        if ((availabilityType == LivenessKey && newState is not LivenessState) || (availabilityType == ReadinessKey && newState is not ReadinessState))
        {
            throw new InvalidOperationException($"{availabilityType} state can only be of type {availabilityType}State");
        }

        _logger.LogTrace("{StateKey} availability has been set to {NewState} by {Caller}", availabilityType, newState, caller ?? "unspecified");
        _availabilityStates[availabilityType] = newState;

        if (availabilityType == LivenessKey)
        {
            LivenessChanged?.Invoke(this, new AvailabilityEventArgs(newState));
        }

        if (availabilityType == ReadinessKey)
        {
            ReadinessChanged?.Invoke(this, new AvailabilityEventArgs(newState));
        }
    }
}
