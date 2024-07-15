// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Health.Availability;

/// <summary>
/// "Liveness" state of the application.
/// <para />
/// An application is considered live when it's running with a correct internal state. "Liveness" failure means that the internal state of the
/// application is broken, and we cannot recover from it. As a result, the platform should restart the application.
/// </summary>
public sealed class LivenessState : AvailabilityState
{
    /// <summary>
    /// The application is running and its internal state is correct.
    /// </summary>
    public static readonly LivenessState Correct = new("CORRECT");

    /// <summary>
    /// The application is running but its internal state is broken.
    /// </summary>
    public static readonly LivenessState Broken = new("BROKEN");

    private LivenessState(string value)
        : base(value)
    {
    }
}
