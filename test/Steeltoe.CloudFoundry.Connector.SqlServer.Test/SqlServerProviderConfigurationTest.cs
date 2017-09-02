using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.Test
{
    public class SqlServerProviderConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
                {
                    ["SqlServer:credentials:uid"] = "username",
                    ["SqlServer:credentials:uri"] = "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e",
                    ["SqlServer:credentials:db"] = "de5aa3a747c134b3d8780f8cc80be519e",
                    ["SqlServer:credentials:pw"] = "password"
                };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new SqlServerProviderConnectorOptions(config);
            Assert.Equal("servername", sconfig.Server);
            Assert.Equal(1433, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.ConnectionString);
        }
    }
}
