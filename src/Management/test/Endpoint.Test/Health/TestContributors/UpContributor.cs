// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health.TestContributors;

internal sealed class UpContributor : IHealthContributor
{
    private readonly int? _sleepyTime;

    public string Id => "Up";

    internal UpContributor(int? sleepyTime = null)
    {
        _sleepyTime = sleepyTime;
    }

    public HealthCheckResult Health()
    {
        if (_sleepyTime != null)
        {
            Thread.Sleep((int)_sleepyTime);
        }

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
