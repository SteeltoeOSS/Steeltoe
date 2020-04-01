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
using System;
using Xunit;

namespace Steeltoe.Connector.SqlServer.EF6.Test
{
    public class SqlServerDbContextConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfTypeNull()
        {
            // Arrange
            var config = new SqlServerProviderConnectorOptions();
            SqlServerServiceInfo si = null;
            Type dbContextType = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new SqlServerDbContextConnectorFactory(si, config, dbContextType));
            Assert.Contains(nameof(dbContextType), ex.Message);
        }

        [Fact]
        public void Create_ThrowsIfNoValidConstructorFound()
        {
            // Arrange
            var config = new SqlServerProviderConnectorOptions();
            SqlServerServiceInfo si = null;
            var dbContextType = typeof(BadSqlServerDbContext);

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => new SqlServerDbContextConnectorFactory(si, config, dbContextType).Create(null));
            Assert.Contains("BadSqlServerDbContext", ex.Message);
        }

        [Fact]
        public void Create_ReturnsDbContext()
        {
            var config = new SqlServerProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1433,
                Password = "password",
                Username = "username",
                Database = "database"
            };
            var si = new SqlServerServiceInfo("MyId", "SqlServer://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
            var factory = new SqlServerDbContextConnectorFactory(si, config, typeof(GoodSqlServerDbContext));
            var context = factory.Create(null);
            Assert.NotNull(context);
            var gcontext = context as GoodSqlServerDbContext;
            Assert.NotNull(gcontext);
        }
    }
}
