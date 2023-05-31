// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connectors.EntityFrameworkCore.MySql.DynamicTypeAccess;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;
using Xunit;
using SteeltoeExtensions = Steeltoe.Connectors.EntityFrameworkCore.MySql.MySqlDbContextOptionsBuilderExtensions;

namespace Steeltoe.Connectors.EntityFrameworkCore.Test.MySql.Oracle;

public sealed class MySqlDbContextOptionsBuilderExtensionsTest
{
    [Fact]
    public async Task Registers_connection_string_for_default_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:Default:ConnectionString"] = "SERVER=localhost;database=myDb;UID=steeltoe;PWD=steeltoe;connect timeout=15"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyOracle());
        builder.Services.Configure<MySqlOptions>(options => options.ConnectionString += ";Use Compression=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => SteeltoeExtensions.UseMySql(options, serviceProvider,
            MySqlEntityFrameworkCorePackageResolver.CreateForOnlyOracle()));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("server=localhost;database=myDb;user id=steeltoe;password=steeltoe;connectiontimeout=15;Use Compression=false");
    }

    [Fact]
    public async Task Registers_connection_string_for_named_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:MySql:myMySqlService:ConnectionString"] = "SERVER=localhost;database=myDb;UID=steeltoe;PWD=steeltoe;connect timeout=15"
        });

        builder.AddMySql(MySqlPackageResolver.CreateForOnlyOracle());
        builder.Services.Configure<MySqlOptions>("myMySqlService", options => options.ConnectionString += ";Use Compression=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => SteeltoeExtensions.UseMySql(options, serviceProvider,
            MySqlEntityFrameworkCorePackageResolver.CreateForOnlyOracle(), "myMySqlService"));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("server=localhost;database=myDb;user id=steeltoe;password=steeltoe;connectiontimeout=15;Use Compression=false");
    }
}
