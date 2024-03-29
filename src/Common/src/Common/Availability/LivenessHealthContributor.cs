// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Common.Availability;

public class LivenessHealthContributor : AvailabilityHealthContributor
{
    private readonly ApplicationAvailability _availability;

    public override string Id => "liveness";

    public LivenessHealthContributor(ApplicationAvailability availability)
        : base(new Dictionary<IAvailabilityState, HealthStatus>
        {
            { LivenessState.Correct, HealthStatus.Up },
            { LivenessState.Broken, HealthStatus.Down }
        })
    {
        _availability = availability;
    }

    protected override IAvailabilityState GetState()
    {
        return _availability.GetLivenessState();
    }
}
