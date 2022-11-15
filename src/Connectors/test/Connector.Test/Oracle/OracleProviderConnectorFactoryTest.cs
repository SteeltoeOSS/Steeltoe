// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Oracle.ManagedDataAccess.Client;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Test.Oracle;

public class OracleProviderConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const OracleProviderConnectorOptions options = null;
        const OracleServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new OracleProviderConnectorFactory(si, options, typeof(OracleConnection)));
        Assert.Contains(nameof(options), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsMySqlConnection()
    {
        var options = new OracleProviderConnectorOptions
        {
            Server = "localhost",
            Port = 3306,
            Password = "password",
            Username = "username",
            ServiceName = "database"
        };

        var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");
        var factory = new OracleProviderConnectorFactory(si, options, typeof(OracleConnection));
        object connection = factory.Create(null);
        Assert.NotNull(connection);
    }
}
