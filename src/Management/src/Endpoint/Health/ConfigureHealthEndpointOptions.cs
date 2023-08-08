// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class ConfigureHealthEndpointOptions : IConfigureOptions<HealthEndpointOptions>
{
    private const string HealthOptionsPrefix = "management:endpoints:health";
    private readonly IConfiguration _configuration;

    public ConfigureHealthEndpointOptions(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public void Configure(HealthEndpointOptions options)
    {
        ArgumentGuard.NotNull(options);

        _configuration.GetSection(HealthOptionsPrefix).Bind(options);

        options.Id ??= "health";

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }

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
