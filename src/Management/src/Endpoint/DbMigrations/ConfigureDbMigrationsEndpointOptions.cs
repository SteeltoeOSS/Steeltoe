// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.DbMigrations;

public class ConfigureDbMigrationsEndpointOptions : ConfigureEndpointOptions<DbMigrationsEndpointOptions>// AbstractEndpointOptions, IDbMigrationsOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:dbmigrations";
    public ConfigureDbMigrationsEndpointOptions(IConfiguration configuration) : base(configuration, ManagementInfoPrefix, "dbmigrations")
    {
    }
}