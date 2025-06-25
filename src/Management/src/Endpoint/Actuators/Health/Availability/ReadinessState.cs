// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health.Availability;

/// <summary>
/// The Readiness state of the application.
/// <para />
/// An application is considered ready when it's <see cref="LivenessState.Correct" /> and willing to accept traffic. "Readiness" failure means that the
/// application is not able to accept traffic and that the infrastructure should not route requests to it.
/// </summary>
public sealed class ReadinessState : AvailabilityState
{
    /// <summary>
    /// The application is ready to receive traffic.
    /// </summary>
    public static readonly ReadinessState AcceptingTraffic = new("ACCEPTING_TRAFFIC");

    /// <summary>
    /// The application is not willing to receive traffic.
    /// </summary>
    public static readonly ReadinessState RefusingTraffic = new("REFUSING_TRAFFIC");

    private ReadinessState(string value)
        : base(value)
    {
    }
}
