// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connectors.EntityFrameworkCore.SqlServer;
using Steeltoe.Connectors.SqlServer;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;
using Xunit;

namespace Steeltoe.Connectors.EntityFrameworkCore.Test.SqlServer;

public sealed class SqlServerDbContextOptionsBuilderExtensionsTest
{
    [Fact]
    public async Task Registers_connection_string_for_default_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:Default:ConnectionString"] = "SERVER=localhost;database=myDb;UID=steeltoe;PWD=steeltoe;Max Pool Size=50"
        });

        builder.AddSqlServer(SqlServerPackageResolver.CreateForOnlyMicrosoftData());
        builder.Services.Configure<SqlServerOptions>(options => options.ConnectionString += ";Encrypt=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseSqlServer(serviceProvider));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("Data Source=localhost;Initial Catalog=myDb;User ID=steeltoe;Password=steeltoe;Max Pool Size=50;Encrypt=false");
    }

    [Fact]
    public async Task Registers_connection_string_for_named_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:SqlServer:mySqlServerService:ConnectionString"] = "SERVER=localhost;database=myDb;UID=steeltoe;PWD=steeltoe;Max Pool Size=50"
        });

        builder.AddSqlServer(SqlServerPackageResolver.CreateForOnlyMicrosoftData());
        builder.Services.Configure<SqlServerOptions>("mySqlServerService", options => options.ConnectionString += ";Encrypt=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseSqlServer(serviceProvider, "mySqlServerService"));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("Data Source=localhost;Initial Catalog=myDb;User ID=steeltoe;Password=steeltoe;Max Pool Size=50;Encrypt=false");
    }
}
