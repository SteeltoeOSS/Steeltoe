// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Availability;

public sealed class ReadinessHealthContributorTest
{
    private readonly ApplicationAvailability _availability = new(NullLogger<ApplicationAvailability>.Instance);

    [Fact]
    public async Task HandlesUnknown()
    {
        var contributor = new ReadinessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task HandlesAccepting()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
    }

    [Fact]
    public async Task HandlesRefusing()
    {
        _availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, "tests");
        var contributor = new ReadinessHealthContributor(_availability, NullLoggerFactory.Instance);

        HealthCheckResult? result = await contributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.OutOfService, result.Status);
    }
}
