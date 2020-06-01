// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Data.SqlClient;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.Test
{
    public class SqlServerProviderConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            SqlServerProviderConnectorOptions config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerProviderConnectorFactory(null, config, typeof(SqlConnection)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsSqlConnection()
        {
            SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions()
            {
                Server = "servername",
                Password = "password",
                Username = "username",
                Database = "database"
            };
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e", "user", "pass");
            var factory = new SqlServerProviderConnectorFactory(si, config, typeof(SqlConnection));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
