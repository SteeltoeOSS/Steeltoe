// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class OutOfServiceContributor : IHealthContributor
{
    public string Id => "Out";

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<HealthCheckResult?>(new HealthCheckResult
        {
            Status = HealthStatus.OutOfService
        });
    }
}
