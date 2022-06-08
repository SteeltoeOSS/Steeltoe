// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System.Collections.Generic;

namespace Steeltoe.Common.Availability;

public class LivenessHealthContributor : AvailabilityHealthContributor
{
    public override string Id => "liveness";

    private readonly ApplicationAvailability _availability;

    public LivenessHealthContributor(ApplicationAvailability availability)
        : base(new Dictionary<IAvailabilityState, HealthStatus> { { LivenessState.Correct, HealthStatus.UP }, { LivenessState.Broken, HealthStatus.DOWN } })
    {
        _availability = availability;
    }

    protected override IAvailabilityState GetState() => _availability.GetLivenessState();
}
