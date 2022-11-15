// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Npgsql;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Test.PostgreSQL;

public class PostgreSqlProviderConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const PostgreSqlProviderConnectorOptions options = null;
        const PostgreSqlServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new PostgreSqlProviderConnectorFactory(si, options, typeof(NpgsqlConnection)));
        Assert.Contains(nameof(options), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsPostgreSqlConnection()
    {
        var options = new PostgreSqlProviderConnectorOptions
        {
            Host = "localhost",
            Port = 3306,
            Password = "password",
            Username = "username",
            Database = "database"
        };

        var si = new PostgreSqlServiceInfo("MyId", "postgres://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new PostgreSqlProviderConnectorFactory(si, options, typeof(NpgsqlConnection));
        object connection = factory.Create(null);
        Assert.NotNull(connection);
    }
}
