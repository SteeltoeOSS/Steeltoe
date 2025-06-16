// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class ThrowingContributor : IHealthContributor
{
    public string Id => "alwaysThrowing";

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }
}
