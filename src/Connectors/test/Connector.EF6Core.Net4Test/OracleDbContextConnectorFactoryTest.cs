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

using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.ConnectorBase.Relational.Oracle;
using Steeltoe.CloudFoundry.ConnectorBase.Relational.Oracle.EF6;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Oracle.EF6.Test
{
    public class OracleDbContextConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfTypeNull()
        {
            // Arrange
            OracleProviderConnectorOptions config = new OracleProviderConnectorOptions();
            OracleServiceInfo si = null;
            Type dbContextType = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new OracleDbContextConnectorFactory(si, config, dbContextType));
            Assert.Contains(nameof(dbContextType), ex.Message);
        }

        [Fact]
        public void Create_ThrowsIfNoValidConstructorFound()
        {
            // Arrange
            OracleProviderConnectorOptions config = new OracleProviderConnectorOptions();
            OracleServiceInfo si = null;
            Type dbContextType = typeof(BadOracleDbContext);

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => new OracleDbContextConnectorFactory(si, config, dbContextType).Create(null));
            Assert.Contains("BadOracleDbContext", ex.Message);
        }

        [Fact]
        public void Create_ReturnsDbContext()
        {
            OracleProviderConnectorOptions config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1521,
                Password = "I2rK7m8vGPs=1",
                Username = "SYSTEM",
                ServiceName= "ORCLCDB"
            };
            OracleServiceInfo si = new OracleServiceInfo("MyId", "Oracle://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
            var factory = new OracleDbContextConnectorFactory(si, config, typeof(GoodOracleDbContext));
            var context = factory.Create(null);
            Assert.NotNull(context);
            GoodOracleDbContext gcontext = context as GoodOracleDbContext;
            Assert.NotNull(gcontext);
        }
    }
}
