// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health.TestContributors;

internal sealed class UpContributor : IHealthContributor
{
    private readonly int? _sleepTime;

    public string Id => "Up";

    internal UpContributor(int? sleepTime = null)
    {
        _sleepTime = sleepTime;
    }

    public HealthCheckResult Health()
    {
        if (_sleepTime != null)
        {
            Thread.Sleep((int)_sleepTime);
        }

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
