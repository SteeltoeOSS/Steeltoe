using Steeltoe.CloudFoundry.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.Test
{
    public class SqlServerProviderConfigurerTest
    {
        // shared variable to hold config (like from a source such as appsettings)
        SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions()
        {
            Server = "localhost",
            Username = "username",
            Password = "password",
            Database = "database"
        };

        [Fact]
        public void UpdateConfiguration_WithNullSqlServerServiceInfo_ReturnsExpected()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            configurer.UpdateConfiguration(null, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal("username", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Equal("database", config.Database);
            Assert.Null(config.ConnectionString);

        }

        [Fact]
        public void UpdateConfiguration_WithSqlServiceInfo_ReturnsExpected()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("servername", config.Server);
            Assert.Equal("Dd6O1BPXUHdrmzbP", config.Username);
            Assert.Equal("7E1LxXnlH2hhlPVt", config.Password);
            Assert.Equal("de5aa3a747c134b3d8780f8cc80be519e", config.Database);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            var opts = configurer.Configure(null, config);
            Assert.Contains("Server=localhost;", opts);
            Assert.Contains("Username=username;", opts);
            Assert.Contains("Password=password;", opts);
            Assert.Contains("Database=database;", opts);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            
            // override provided by environment
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e");

            // apply override
            var opts = configurer.Configure(si, config);

            // resulting options should contain values parsed from environment
            Assert.Contains("Server=servername;", opts);
            Assert.Contains("Username=Dd6O1BPXUHdrmzbP;", opts);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
            Assert.Contains("Database=de5aa3a747c134b3d8780f8cc80be519e;", opts);
        }
    }
}
