// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointOptions : AbstractEndpointOptions, IHealthOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:health";

    public ShowDetails ShowDetails { get; set; }

    public EndpointClaim Claim { get; set; }

    public string Role { get; set; }

    public Dictionary<string, HealthGroupOptions> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HealthEndpointOptions()
    {
        Id = "health";
        RequiredPermissions = Permissions.Restricted;
        ExactMatch = false;

        AddDefaultGroups();
    }

    public HealthEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "health";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }

        if (Claim == null && !string.IsNullOrEmpty(Role))
        {
            Claim = new EndpointClaim
            {
                Type = ClaimTypes.Role,
                Value = Role
            };
        }

        ExactMatch = false;

        AddDefaultGroups();
    }

    private void AddDefaultGroups()
    {
        if (!Groups.ContainsKey("liveness"))
        {
            Groups.Add("liveness", new HealthGroupOptions
            {
                Include = "liveness"
            });
        }

        if (!Groups.ContainsKey("readiness"))
        {
            Groups.Add("readiness", new HealthGroupOptions
            {
                Include = "readiness"
            });
        }
    }
}
