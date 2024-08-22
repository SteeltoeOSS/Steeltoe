// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class UpContributor : IHealthContributor
{
    private readonly int? _sleepTime;

    public string Id => "Up";

    internal UpContributor(int? sleepTime = null)
    {
        _sleepTime = sleepTime;
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        if (_sleepTime != null)
        {
            await Task.Delay(_sleepTime.Value, cancellationToken);
        }

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
