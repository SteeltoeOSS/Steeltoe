// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal sealed class PingContributor : IHealthContributor
{
    private readonly IOptionsMonitor<PingContributorOptions> _optionsMonitor;

    public string Id => "ping";

    public PingContributor(IOptionsMonitor<PingContributorOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        HealthCheckResult? result = Health();
        return Task.FromResult(result);
    }

    private HealthCheckResult? Health()
    {
        PingContributorOptions options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            return null;
        }

        return new HealthCheckResult
        {
            Status = HealthStatus.Up
        };
    }
}
