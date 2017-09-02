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
            Port = 1433,
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
        public void Update_With_ServiceInfo_Updates_Config()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://updatedserver:1433;databaseName=udpateddb", "updateduser", "updatedpassword");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("updatedserver", config.Server);
            Assert.Equal("udpateddb", config.Database);
            Assert.Equal("updateduser", config.Username);
            Assert.Equal("updatedpassword", config.Password);
        }

        [Fact]
        public void Configure_Without_ServiceInfo_Returns_Config()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            var opts = configurer.Configure(null, config);
            Assert.Contains("Data Source=localhost", opts);
            Assert.Contains("User Id=username;", opts);
            Assert.Contains("Password=password;", opts);
            Assert.Contains("Initial Catalog=database;", opts);
        }

        [Fact]
        public void Configure_With_ServiceInfo_Overrides_Config()
        {
            SqlServerProviderConfigurer configurer = new SqlServerProviderConfigurer();
            
            // override provided by environment
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");

            // apply override
            var opts = configurer.Configure(si, config);

            // resulting options should contain values parsed from environment
            Assert.Contains("Data Source=servername", opts);
            Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
            Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
        }
    }
}
