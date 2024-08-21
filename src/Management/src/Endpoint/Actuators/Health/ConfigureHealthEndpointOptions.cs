// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class ConfigureHealthEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<HealthEndpointOptions>(configuration, HealthOptionsPrefix, "health")
{
    private const string HealthOptionsPrefix = "management:endpoints:health";

    public override void Configure(HealthEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        base.Configure(options);

        if (options.Claim == null && !string.IsNullOrEmpty(options.Role))
        {
            options.Claim = new EndpointClaim
            {
                Type = ClaimTypes.Role,
                Value = options.Role
            };
        }

        options.Groups.TryAdd("liveness", new HealthGroupOptions
        {
            Include = "liveness"
        });

        options.Groups.TryAdd("readiness", new HealthGroupOptions
        {
            Include = "readiness"
        });
    }
}
