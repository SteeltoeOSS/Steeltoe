// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.MySql;
using Xunit;
using SteeltoeExtensions = Steeltoe.Connector.EntityFrameworkCore.MySql.MySqlDbContextOptionsBuilderExtensions;

namespace Steeltoe.Connector.EntityFrameworkCore.Test;

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

        builder.AddMySql();
        builder.Services.Configure<MySqlOptions>(options => options.ConnectionString += ";Use Compression=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) =>
            SteeltoeExtensions.UseMySql(options, serviceProvider, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));

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

        builder.AddMySql();
        builder.Services.Configure<MySqlOptions>("myMySqlService", options => options.ConnectionString += ";Use Compression=false");

        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) =>
            SteeltoeExtensions.UseMySql(options, serviceProvider, "myMySqlService", MySqlServerVersion.LatestSupportedServerVersion));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("server=localhost;database=myDb;user id=steeltoe;password=steeltoe;connectiontimeout=15;Use Compression=false");
    }

    [Fact]
    public async Task Throws_for_unknown_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMySql();
        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => SteeltoeExtensions.UseMySql(options, serviceProvider, "unknownService"));

        await using WebApplication app = builder.Build();

        Action action = () => app.Services.GetRequiredService<GoodDbContext>();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Connection string for service binding 'unknownService' not found.");
    }
}
