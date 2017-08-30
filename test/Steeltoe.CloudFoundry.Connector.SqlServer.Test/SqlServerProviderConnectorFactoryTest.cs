using Microsoft.EntityFrameworkCore.Storage.Internal;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
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
            var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerProviderConnectorFactory(null, config, typeof(SqlServerConnection)));
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
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername;databaseName=de5aa3a747c134b3d8780f8cc80be519e");
            var factory = new SqlServerProviderConnectorFactory(si, config, typeof(SqlServerConnection));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
