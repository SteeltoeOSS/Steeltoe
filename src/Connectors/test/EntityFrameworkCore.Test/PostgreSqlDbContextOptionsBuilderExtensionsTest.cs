// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.EntityFrameworkCore.PostgreSql;
using Steeltoe.Connector.PostgreSql;
using Xunit;

namespace Steeltoe.Connector.EntityFrameworkCore.Test;

public sealed class PostgreSqlDbContextOptionsBuilderExtensionsTest
{
    [Fact]
    public async Task Registers_connection_string_for_default_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:Default:ConnectionString"] = "SERVER=localhost;DB=myDb;UID=myUser;PWD=myPass;Log Parameters=True"
        });

        builder.AddPostgreSql();
        builder.Services.Configure<PostgreSqlOptions>(options => options.ConnectionString += ";Include Error Detail=true");
        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("Host=localhost;Database=myDb;Username=myUser;Password=myPass;Log Parameters=True;Include Error Detail=true");
    }

    [Fact]
    public async Task Registers_connection_string_for_named_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Steeltoe:Client:PostgreSql:myPostgreSqlService:ConnectionString"] = "SERVER=localhost;DB=myDb;UID=myUser;PWD=myPass;Log Parameters=True"
        });

        builder.AddPostgreSql();
        builder.Services.Configure<PostgreSqlOptions>("myPostgreSqlService", options => options.ConnectionString += ";Include Error Detail=true");
        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider, "myPostgreSqlService"));

        await using WebApplication app = builder.Build();

        await using var dbContext = app.Services.GetRequiredService<GoodDbContext>();
        string connectionString = dbContext.Database.GetConnectionString();

        connectionString.Should().Be("Host=localhost;Database=myDb;Username=myUser;Password=myPass;Log Parameters=True;Include Error Detail=true");
    }

    [Fact]
    public async Task Throws_for_unknown_service_binding()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddPostgreSql();
        builder.Services.AddDbContext<GoodDbContext>((serviceProvider, options) => options.UseNpgsql(serviceProvider, "unknownService"));

        await using WebApplication app = builder.Build();

        Action action = () => app.Services.GetRequiredService<GoodDbContext>();
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("Connection string for service binding 'unknownService' not found.");
    }
}
