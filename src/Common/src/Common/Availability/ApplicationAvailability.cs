// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Availability;

public class ApplicationAvailability
{
    public const string LivenessKey = "Liveness";
    public const string ReadinessKey = "Readiness";
    private readonly Dictionary<string, IAvailabilityState> _availabilityStates = new();
    private readonly ILogger<ApplicationAvailability> _logger;

    public event EventHandler LivenessChanged;

    public event EventHandler ReadinessChanged;

    public ApplicationAvailability(ILogger<ApplicationAvailability> logger = null)
    {
        _logger = logger;
    }

    public IAvailabilityState GetLivenessState()
    {
        return GetAvailabilityState(LivenessKey);
    }

    public IAvailabilityState GetReadinessState()
    {
        return GetAvailabilityState(ReadinessKey);
    }

    public IAvailabilityState GetAvailabilityState(string availabilityType)
    {
        try
        {
            return _availabilityStates[availabilityType];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Set the availability state for a given availability type.
    /// </summary>
    /// <param name="stateKey">
    /// String name for the type of state to set. See <see cref="LivenessKey" /> or <see cref="ReadinessKey" /> for convenience.
    /// </param>
    /// <param name="newState">
    /// The new <see cref="IAvailabilityState" />.
    /// </param>
    /// <param name="caller">
    /// Logged at trace level for tracking origin of state change.
    /// </param>
    public void SetAvailabilityState(string stateKey, IAvailabilityState newState, string caller)
    {
        if ((stateKey.Equals(LivenessKey) && newState is not LivenessState) || (stateKey.Equals(ReadinessKey) && newState is not ReadinessState))
        {
            throw new InvalidOperationException($"{stateKey} state can only be of type {stateKey}State");
        }

        _logger?.LogTrace("{stateKey} availability has been set to {newState} by {caller}", stateKey, newState, caller ?? "unspecified");
        _availabilityStates[stateKey] = newState;

        if (stateKey == LivenessKey)
        {
            LivenessChanged?.Invoke(this, new AvailabilityEventArgs(newState));
        }

        if (stateKey == ReadinessKey)
        {
            ReadinessChanged?.Invoke(this, new AvailabilityEventArgs(newState));
        }
    }
}
