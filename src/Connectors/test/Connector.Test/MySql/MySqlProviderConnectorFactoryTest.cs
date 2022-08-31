// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MySqlConnector;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.MySql.Test;

public class MySqlProviderConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const MySqlProviderConnectorOptions options = null;
        const MySqlServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new MySqlProviderConnectorFactory(si, options, typeof(MySqlConnection)));
        Assert.Contains(nameof(options), ex.Message);
    }

    [Fact]
    public void Create_ReturnsMySqlConnection()
    {
        var options = new MySqlProviderConnectorOptions
        {
            Server = "localhost",
            Port = 3306,
            Password = "password",
            Username = "username",
            Database = "database"
        };

        var si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new MySqlProviderConnectorFactory(si, options, typeof(MySqlConnection));
        object connection = factory.Create(null);
        Assert.NotNull(connection);
    }
}
