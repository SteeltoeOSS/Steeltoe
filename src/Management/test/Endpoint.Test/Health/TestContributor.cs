// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health;

internal sealed class TestContributor : IHealthContributor
{
    public string Id => "Test";

    public HealthCheckResult Health()
    {
        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
