// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    public class MySqlProviderConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MySqlProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["mysql:client:server"] = "localhost",
                ["mysql:client:port"] = "1234",
                ["mysql:client:password"] = "password",
                ["mysql:client:username"] = "username"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new MySqlProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
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
                ["mysql:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var sconfig = new MySqlProviderConnectorOptions(config);

            // assert
            Assert.Equal(appsettings["mysql:client:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig()
        {
            // arrange
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>()
            {
                ["mysql:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVCAP);

            // add settings to config
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new MySqlProviderConnectorOptions(config);

            // assert
            Assert.NotEqual(appsettings["mysql:client:ConnectionString"], sconfig.ToString());
        }
    }
}
