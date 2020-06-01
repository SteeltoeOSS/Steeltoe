// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.Endpoint.Health
{
    [Obsolete("Use HealthEndpointOptions instead.")]
    public class HealthOptions : AbstractOptions, IHealthOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health";

        public HealthOptions()
            : base()
        {
            Id = "health";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public HealthOptions(IConfiguration config)
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
        }

        public ShowDetails ShowDetails { get; set; }

        public EndpointClaim Claim { get; set; }

        public string Role { get; set; }
    }
}
