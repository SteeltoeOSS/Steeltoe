// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class PostConfigureHealthEndpointOptions(
    IOptions<LivenessStateContributorOptions> livenessOptions, IOptions<ReadinessStateContributorOptions> readinessOptions)
    : IPostConfigureOptions<HealthEndpointOptions>
{
    public void PostConfigure(string? name, HealthEndpointOptions options)
    {
        if (livenessOptions.Value.Enabled && !options.Groups.ContainsKey(LivenessStateContributorOptions.GroupName))
        {
            options.Groups.Add(LivenessStateContributorOptions.GroupName, new HealthGroupOptions
            {
                Include = "livenessState"
            });
        }

        if (readinessOptions.Value.Enabled && !options.Groups.ContainsKey(ReadinessStateContributorOptions.HealthGroupName))
        {
            options.Groups.Add(ReadinessStateContributorOptions.HealthGroupName, new HealthGroupOptions
            {
                Include = "readinessState"
            });
        }
    }
}
