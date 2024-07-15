// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health.Availability;

public sealed class LivenessHealthContributor : AvailabilityHealthContributor
{
    private readonly ApplicationAvailability _availability;

    public override string Id => "liveness";

    public LivenessHealthContributor(ApplicationAvailability availability, ILoggerFactory loggerFactory)
        : base(new Dictionary<AvailabilityState, HealthStatus>
        {
            { LivenessState.Correct, HealthStatus.Up },
            { LivenessState.Broken, HealthStatus.Down }
        }, loggerFactory)
    {
        ArgumentGuard.NotNull(availability);

        _availability = availability;
    }

    protected override AvailabilityState? GetState()
    {
        return _availability.GetLivenessState();
    }
}
