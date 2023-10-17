// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class ConfigureHealthEndpointOptions : ConfigureEndpointOptions<HealthEndpointOptions>
{
    private const string HealthOptionsPrefix = "management:endpoints:health";

    public ConfigureHealthEndpointOptions(IConfiguration configuration)
        : base(configuration, HealthOptionsPrefix, "health")
    {
    }

    public override void Configure(HealthEndpointOptions options)
    {
        ArgumentGuard.NotNull(options);

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
