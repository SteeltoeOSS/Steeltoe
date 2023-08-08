// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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
        services.AddDbMigrationsActuator();
        var scanner = Substitute.For<DbMigrationsEndpointHandler.DatabaseMigrationScanner>();

        scanner.GetPendingMigrations(Arg.Any<DbContext>()).Returns(new[]
        {
            "pending"
        });

        scanner.GetAppliedMigrations(Arg.Any<DbContext>()).Returns(new[]
        {
            "applied"
        });

        scanner.ScanRootAssembly.Returns(typeof(MockDbContext).Assembly);
        services.AddSingleton(scanner);
        services.AddRouting();
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
