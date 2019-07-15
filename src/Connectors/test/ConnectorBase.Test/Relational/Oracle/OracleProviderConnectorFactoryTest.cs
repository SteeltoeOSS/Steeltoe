// Copyright 2019 Infosys Ltd.
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

using Oracle.ManagedDataAccess.Client;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.ConnectorBase.Relational.Oracle;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorBase.Test.Relational.Oracle
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
            OracleProviderConnectorOptions config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 3306,
                Password = "password",
                Username = "username",
                ServiceName = "database"
            };
            OracleServiceInfo si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");
            var factory = new OracleProviderConnectorFactory(si, config, typeof(OracleConnection));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
