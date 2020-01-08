// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.Test
{
    public class SqlServerProviderConfigurerTest
    {
        // shared variable to hold config (like from a source such as appsettings)
        private SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions()
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
            var configurer = new SqlServerProviderConfigurer();
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
            var configurer = new SqlServerProviderConfigurer();
            var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://updatedserver:1433/databaseName=updateddb", "updateduser", "updatedpassword");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("updatedserver", config.Server);
            Assert.Equal("updateddb", config.Database);
            Assert.Equal("updateduser", config.Username);
            Assert.Equal("updatedpassword", config.Password);
        }

        [Fact]
        public void Update_With_ServiceInfo_CredsInUrl_Updates_Config()
        {
            var configurer = new SqlServerProviderConfigurer();
            var si = new SqlServerServiceInfo("MyId", "sqlserver://updateduser:updatedpassword@updatedserver:1433;databaseName=updateddb");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("updatedserver", config.Server);
            Assert.Equal("updateddb", config.Database);
            Assert.Equal("updateduser", config.Username);
            Assert.Equal("updatedpassword", config.Password);
        }

        [Fact]
        public void Configure_Without_ServiceInfo_Returns_Config()
        {
            var configurer = new SqlServerProviderConfigurer();
            var opts = configurer.Configure(null, config);
            Assert.Contains("Data Source=localhost,1433", opts);
            Assert.Contains("User Id=username;", opts);
            Assert.Contains("Password=password;", opts);
            Assert.Contains("Initial Catalog=database;", opts);
        }

        [Fact]
        public void Configure_With_ServiceInfo_Overrides_Config()
        {
            var configurer = new SqlServerProviderConfigurer();

            // override provided by environment
            var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");

            // apply override
            var opts = configurer.Configure(si, config);

            // resulting options should contain values parsed from environment
            Assert.Contains("Data Source=servername,1433", opts);
            Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
            Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
        }

        [Fact]
        public void Configure_With_ServiceInfo_CredsInUrl_Overrides_Config()
        {
            var configurer = new SqlServerProviderConfigurer();

            // override provided by environment
            var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@servername:1433/databaseName=de5aa3a747c134b3d8780f8cc80be519e");

            // apply override
            var opts = configurer.Configure(si, config);

            // resulting options should contain values parsed from environment
            Assert.Contains("Data Source=servername,1433", opts);
            Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
            Assert.Contains("User Id=Dd6O1BPXUHdrmzbP;", opts);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt;", opts);
        }

        [Fact]
        public void Configure_With_ServiceInfo_NamedInstance_Overrides_Config()
        {
            var configurer = new SqlServerProviderConfigurer();

            // override provided by environment
            var si = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://servername/databaseName=de5aa3a747c134b3d8780f8cc80be519e;instanceName=someInstance;integratedSecurity=true");

            // apply override
            var opts = configurer.Configure(si, config);

            // resulting options should contain values parsed from environment
            Assert.Contains("Data Source=servername\\someInstance", opts);
            Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e;", opts);
            Assert.Contains("integratedSecurity=true;", opts);
        }
    }
}
