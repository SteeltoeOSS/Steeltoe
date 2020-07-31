// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Oracle.ManagedDataAccess.Client;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Oracle.Test
{
    public class OracleProviderConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            OracleProviderConnectorOptions config = null;
            OracleServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new OracleProviderConnectorFactory(si, config, typeof(OracleConnection)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsMySqlConnection()
        {
            var config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 3306,
                Password = "password",
                Username = "username",
                ServiceName = "database"
            };
            var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");
            var factory = new OracleProviderConnectorFactory(si, config, typeof(OracleConnection));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
