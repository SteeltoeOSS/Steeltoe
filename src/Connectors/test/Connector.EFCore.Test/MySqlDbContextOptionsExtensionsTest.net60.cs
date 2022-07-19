// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Steeltoe.Connector.EFCore.Test;
using Xunit;

namespace Steeltoe.Connector.MySql.EFCore.Test;

public partial class MySqlDbContextOptionsExtensionsTest
{
    // Run a MySQL server with Docker to match credentials below with this command
    // docker run --name steeltoe-mysql -p 3306:3306 -e MYSQL_DATABASE=steeltoe -e MYSQL_ROOT_PASSWORD=steeltoe mysql
    [Fact(Skip = "Requires a running MySQL server to support AutoDetect")]
    public void AddDbContext_NoVCAPs_AddsDbContext_WithMySqlConnection_AutodetectOn5_0()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "mysql:client:database", "steeltoe2" }, { "mysql:client:username", "root" }, { "mysql:client:password", "steeltoe" } }).Build();

        services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));

        var service = services.BuildServiceProvider().GetService<GoodDbContext>();
        Assert.NotNull(service);
        var con = service.Database.GetDbConnection();
        Assert.NotNull(con);
        Assert.NotNull(con as MySqlConnection);
    }

    private static void AddMySqlDbContext(IServiceCollection services, IConfigurationRoot config, string serviceName = null)
    {
        if (serviceName == null)
        {
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
        }
        else
        {
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, serviceName, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
        }
    }
}
#endif
