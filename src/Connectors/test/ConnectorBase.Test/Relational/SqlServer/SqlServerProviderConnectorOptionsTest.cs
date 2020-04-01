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

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.SqlServer.Test
{
    public class SqlServerProviderConnectorOptionsTest
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
                    ["sqlserver:credentials:uid"] = "username",
                    ["sqlserver:credentials:uri"] = "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e",
                    ["sqlserver:credentials:db"] = "de5aa3a747c134b3d8780f8cc80be519e",
                    ["sqlserver:credentials:pw"] = "password"
                };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new SqlServerProviderConnectorOptions(config);
            Assert.Equal("servername", sconfig.Server);
            Assert.Equal(1433, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.ConnectionString);
        }

        [Fact]
        public void ConnectionString_Returned_AsConfigured()
        {
            // arrange
            var appsettings = new Dictionary<string, string>()
            {
                ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var sconfig = new SqlServerProviderConnectorOptions(config);

            // assert
            Assert.Equal(appsettings["sqlserver:credentials:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig()
        {
            // arrange
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>()
            {
                ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAP);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new SqlServerProviderConnectorOptions(config);

            // assert
            Assert.NotEqual(appsettings["sqlserver:credentials:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void CloudFoundryConfig_Found_By_Name()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAPNoTag);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new SqlServerProviderConnectorOptions(config);

            // assert
            Assert.NotEqual("192.168.0.80", sconfig.Server);
            Assert.NotEqual("de5aa3a747c134b3d8780f8cc80be519e", sconfig.Database);
            Assert.NotEqual("uf33b2b30783a4087948c30f6c3b0c90f", sconfig.Username);
            Assert.NotEqual("Pefbb929c1e0945b5bab5b8f0d110c503", sconfig.Password);
        }

        [Fact]
        public void CloudFoundryConfig_Found_By_Tag()
        {
            // arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAPIgnoreName);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new SqlServerProviderConnectorOptions(config);

            // assert
            Assert.NotEqual("192.168.0.80", sconfig.Server);
            Assert.NotEqual("de5aa3a747c134b3d8780f8cc80be519e", sconfig.Database);
            Assert.NotEqual("uf33b2b30783a4087948c30f6c3b0c90f", sconfig.Username);
            Assert.NotEqual("Pefbb929c1e0945b5bab5b8f0d110c503", sconfig.Password);
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig_CredsInUrl()
        {
            // arrange
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>()
            {
                ["sqlserver:credentials:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAP_CredsInUrl);

            // add settings to config
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new SqlServerProviderConnectorOptions(config);

            // assert
            Assert.NotEqual(appsettings["sqlserver:credentials:ConnectionString"], sconfig.ToString());
        }
    }
}
