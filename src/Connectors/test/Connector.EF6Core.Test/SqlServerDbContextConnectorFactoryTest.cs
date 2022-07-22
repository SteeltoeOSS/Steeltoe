// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.SqlServer.EF6.Test;

public class SqlServerDbContextConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfTypeNull()
    {
        var config = new SqlServerProviderConnectorOptions();
        SqlServerServiceInfo si = null;
        Type dbContextType = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerDbContextConnectorFactory(si, config, dbContextType));
        Assert.Contains(nameof(dbContextType), ex.Message);
    }

    [Fact]
    public void Create_ThrowsIfNoValidConstructorFound()
    {
        var config = new SqlServerProviderConnectorOptions();
        SqlServerServiceInfo si = null;
        var dbContextType = typeof(BadSqlServerDbContext);

        var ex = Assert.Throws<ConnectorException>(() => new SqlServerDbContextConnectorFactory(si, config, dbContextType).Create(null));
        Assert.Contains("BadSqlServerDbContext", ex.Message);
    }

    [Fact]
    public void Create_ReturnsDbContext()
    {
        var config = new SqlServerProviderConnectorOptions()
        {
            Server = "localhost",
            Port = 1433,
            Password = "password",
            Username = "username",
            Database = "database"
        };
        var si = new SqlServerServiceInfo("MyId", "SqlServer://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
        var factory = new SqlServerDbContextConnectorFactory(si, config, typeof(GoodSqlServerDbContext));
        var context = factory.Create(null);
        Assert.NotNull(context);
        var gcontext = context as GoodSqlServerDbContext;
        Assert.NotNull(gcontext);
    }
}