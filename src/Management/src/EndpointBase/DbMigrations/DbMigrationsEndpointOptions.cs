// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Security;
using System;

namespace Steeltoe.Management.EndpointBase.DbMigrations
{
    public class DbMigrationsEndpointOptions : AbstractEndpointOptions, IDbMigrationsOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:dbmigrations";

        public DbMigrationsEndpointOptions()
        {
            Id = "dbmigrations";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public DbMigrationsEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "dbmigrations";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }
        }

        public string[] KeysToSanitize => Array.Empty<string>();
    }
}