// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

public sealed class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<MockDbContext>();
        services.AddCloudFoundryActuator();
        services.AddEntityFrameworkInMemoryDatabase().AddDbContext<MockDbContext>();
        services.AddSingleton<IDatabaseMigrationScanner, TestDatabaseMigrationScanner>();
        services.AddDbMigrationsActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapAllActuators();
        });
    }
}
