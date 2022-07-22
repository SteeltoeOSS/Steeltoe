// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Health.Test;

public class UpContributor : IHealthContributor
{
    public string Id { get; } = "Up";

    private readonly int? _sleepyTime;

    public UpContributor(int? sleepyTime = null)
    {
        _sleepyTime = sleepyTime;
    }

    public HealthCheckResult Health()
    {
        if (_sleepyTime != null)
        {
            Thread.Sleep((int)_sleepyTime);
        }

        return new HealthCheckResult()
        {
            Status = HealthStatus.UP
        };
    }
}