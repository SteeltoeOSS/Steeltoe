// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.Common.Availability.Test;

public class ReadinessHealthContributorTest
{
    private readonly ApplicationAvailability _availability = new ();

    [Fact]
    public void HandlesUnknown()
    {
        var contributor = new ReadinessHealthContributor(_availability);

        var result = contributor.Health();

        Assert.Equal(HealthStatus.UNKNOWN, result.Status);
    }

    [Fact]
    public void HandlesAccepting()
    {
        _availability.SetAvailabilityState(_availability.ReadinessKey, ReadinessState.AcceptingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability);

        var result = contributor.Health();

        Assert.Equal(HealthStatus.UP, result.Status);
    }

    [Fact]
    public void HandlesRefusing()
    {
        _availability.SetAvailabilityState(_availability.ReadinessKey, ReadinessState.RefusingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability);

        var result = contributor.Health();

        Assert.Equal(HealthStatus.OUT_OF_SERVICE, result.Status);
    }
}
