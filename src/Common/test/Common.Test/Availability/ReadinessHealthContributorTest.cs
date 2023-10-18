// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.Common.Test.Availability;

public sealed class ReadinessHealthContributorTest
{
    private readonly ApplicationAvailability _availability = new();

    [Fact]
    public async Task HandlesUnknown()
    {
        var contributor = new ReadinessHealthContributor(_availability);

        HealthCheckResult result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task HandlesAccepting()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability);

        HealthCheckResult result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
    }

    [Fact]
    public async Task HandlesRefusing()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability);

        HealthCheckResult result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.Equal(HealthStatus.OutOfService, result.Status);
    }
}
