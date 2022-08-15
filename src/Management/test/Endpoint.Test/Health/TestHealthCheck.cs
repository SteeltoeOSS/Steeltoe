// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health.Test;

public class TestHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => new HealthCheckResult(HealthStatus.Healthy));
    }
}
