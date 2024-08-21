// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class HealthTestContributor : IHealthContributor
{
    public bool Called { get; private set; }
    public bool Throws { get; }

    public string Id { get; }

    public HealthTestContributor(string id = "Test", bool throws = false)
    {
        Id = id;
        Throws = throws;
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        if (Throws)
        {
            throw new Exception();
        }

        Called = true;

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
