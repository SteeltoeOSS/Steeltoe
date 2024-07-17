// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Availability;

namespace Steeltoe.Management.Endpoint.Test.Health.Availability;

public sealed class LivenessHealthContributorTest
{
    private readonly ApplicationAvailability _availability = new(NullLogger<ApplicationAvailability>.Instance);

    [Fact]
    public async Task HandlesUnknown()
    {
        var contributor = new LivenessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task HandlesCorrect()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, "tests");
        var contributor = new LivenessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
    }

    [Fact]
    public async Task HandlesBroken()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Broken, "tests");
        var contributor = new LivenessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Down, result.Status);
    }
}
