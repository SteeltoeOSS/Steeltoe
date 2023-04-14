// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.EntityFrameworkCore.SqlServer;
using Steeltoe.Connector.SqlServer;
using Xunit;

namespace Steeltoe.Connector.EntityFrameworkCore.Test;

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

        builder.AddSqlServer();
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

        builder.AddSqlServer();
        builder.Services.Configure<SqlServerOptions>("mySqlServerService", options => options.ConnectionString += ";Encrypt=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseSqlServer(serviceProvider, "mySqlServerService"));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("Data Source=localhost;Initial Catalog=myDb;User ID=steeltoe;Password=steeltoe;Max Pool Size=50;Encrypt=false");
    }

    [Fact]
    public async Task Throws_for_unknown_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddSqlServer();

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseSqlServer(serviceProvider, "unknownService"));

        await using WebApplication app = builder.Build();

        Action action = () => app.Services.GetRequiredService<GoodDbContext>();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Connection string for service binding 'unknownService' not found.");
    }
}
