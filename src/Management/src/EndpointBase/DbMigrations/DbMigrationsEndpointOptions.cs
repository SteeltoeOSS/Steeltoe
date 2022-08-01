// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.DbMigrations;

public class DbMigrationsEndpointOptions : AbstractEndpointOptions, IDbMigrationsOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:dbmigrations";

    public DbMigrationsEndpointOptions()
    {
        Id = "dbmigrations";
        RequiredPermissions = Permissions.Restricted;
    }

    public DbMigrationsEndpointOptions(IConfiguration config)
        : base(ManagementInfoPrefix, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "dbmigrations";
        }

        if (RequiredPermissions == Permissions.Undefined)
        {
            RequiredPermissions = Permissions.Restricted;
        }
    }

    public string[] KeysToSanitize => Array.Empty<string>();
}
