// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class PostConfigureHealthEndpointOptions : IPostConfigureOptions<HealthEndpointOptions>
{
    private readonly IOptionsMonitor<LivenessStateContributorOptions> _livenessOptionsMonitor;
    private readonly IOptionsMonitor<ReadinessStateContributorOptions> _readinessOptionsMonitor;

    public PostConfigureHealthEndpointOptions(IOptionsMonitor<LivenessStateContributorOptions> livenessOptionsMonitor,
        IOptionsMonitor<ReadinessStateContributorOptions> readinessOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(livenessOptionsMonitor);
        ArgumentNullException.ThrowIfNull(readinessOptionsMonitor);

        _livenessOptionsMonitor = livenessOptionsMonitor;
        _readinessOptionsMonitor = readinessOptionsMonitor;
    }

    public void PostConfigure(string? name, HealthEndpointOptions options)
    {
        if (_livenessOptionsMonitor.CurrentValue.Enabled && !options.Groups.ContainsKey(LivenessStateContributorOptions.GroupName))
        {
            options.Groups.Add(LivenessStateContributorOptions.GroupName, new HealthGroupOptions
            {
                Include = "livenessState"
            });
        }

        if (_readinessOptionsMonitor.CurrentValue.Enabled && !options.Groups.ContainsKey(ReadinessStateContributorOptions.HealthGroupName))
        {
            options.Groups.Add(ReadinessStateContributorOptions.HealthGroupName, new HealthGroupOptions
            {
                Include = "readinessState"
            });
        }
    }
}
