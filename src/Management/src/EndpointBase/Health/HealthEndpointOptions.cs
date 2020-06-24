// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpointOptions : AbstractEndpointOptions, IHealthOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health";

        public HealthEndpointOptions()
            : base()
        {
            Id = "health";
            RequiredPermissions = Permissions.RESTRICTED;

            AddDefaultGroups();
        }

        public HealthEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "health";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }

            if (Claim == null && !string.IsNullOrEmpty(Role))
            {
                Claim = new EndpointClaim
                {
                    Type = ClaimTypes.Role,
                    Value = Role
                };
            }

            AddDefaultGroups();
        }

        private void AddDefaultGroups()
        {
            if (!Groups.ContainsKey("liveness"))
            {
                Groups.Add("liveness", new HealthGroupOptions { Include = "diskSpace" });
            }

            if (!Groups.ContainsKey("readiness"))
            {
                Groups.Add("readiness", new HealthGroupOptions { Include = "diskSpace" });
            }
        }

        public ShowDetails ShowDetails { get; set; }

        public EndpointClaim Claim { get; set; }

        public string Role { get; set; }

        public Dictionary<string, HealthGroupOptions> Groups { get; set; } = new Dictionary<string, HealthGroupOptions>(StringComparer.InvariantCultureIgnoreCase);
    }
}
