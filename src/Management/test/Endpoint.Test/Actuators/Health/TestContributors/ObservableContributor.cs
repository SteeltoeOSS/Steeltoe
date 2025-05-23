// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

internal sealed class ObservableContributor : IHealthContributor
{
    private int _invocationCount;

    public string Id => "observableAlwaysUp";
    public int InvocationCount => _invocationCount;

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        Interlocked.Increment(ref _invocationCount);

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
