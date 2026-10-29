// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class SlowContributor(TimeProvider timeProvider, TimeSpan sleepTime) : IHealthContributor
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly TimeSpan _sleepTime = sleepTime;

    public string Id => "alwaysSlow";

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(_sleepTime, _timeProvider, cancellationToken);

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
