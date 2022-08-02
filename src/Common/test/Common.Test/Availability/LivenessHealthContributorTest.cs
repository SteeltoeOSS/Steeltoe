// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.Common.Availability.Test;

public class LivenessHealthContributorTest
{
    private readonly ApplicationAvailability _availability = new();

    [Fact]
    public void HandlesUnknown()
    {
        var contributor = new LivenessHealthContributor(_availability);

        HealthCheckResult result = contributor.Health();

        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public void HandlesCorrect()
    {
        _availability.SetAvailabilityState(_availability.LivenessKey, LivenessState.Correct, "tests");
        var contributor = new LivenessHealthContributor(_availability);

        HealthCheckResult result = contributor.Health();

        Assert.Equal(HealthStatus.Up, result.Status);
    }

    [Fact]
    public void HandlesBroken()
    {
        _availability.SetAvailabilityState(_availability.LivenessKey, LivenessState.Broken, "tests");
        var contributor = new LivenessHealthContributor(_availability);

        HealthCheckResult result = contributor.Health();

        Assert.Equal(HealthStatus.Down, result.Status);
    }
}
