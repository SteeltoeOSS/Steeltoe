﻿// Copyright 2017 the original author or authors.
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

#if NET461
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.EF6.Test
{
    public class MySqlDbContextConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfTypeNull()
        {
            // Arrange
            MySqlProviderConnectorOptions config = new MySqlProviderConnectorOptions();
            MySqlServiceInfo si = null;
            Type dbContextType = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MySqlDbContextConnectorFactory(si, config, dbContextType));
            Assert.Contains(nameof(dbContextType), ex.Message);
        }

        [Fact]
        public void Create_ThrowsIfNoValidConstructorFound()
        {
            // Arrange
            MySqlProviderConnectorOptions config = new MySqlProviderConnectorOptions();
            MySqlServiceInfo si = null;
            Type dbContextType = typeof(BadMySqlDbContext);

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => new MySqlDbContextConnectorFactory(si, config, dbContextType).Create(null));
            Assert.Contains("BadMySqlDbContext", ex.Message);
        }

        [Fact]
        public void Create_ReturnsDbContext()
        {
            MySqlProviderConnectorOptions config = new MySqlProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 3306,
                Password = "password",
                Username = "username",
                Database = "database"
            };
            MySqlServiceInfo si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
            var factory = new MySqlDbContextConnectorFactory(si, config, typeof(GoodMySqlDbContext));
            var context = factory.Create(null);
            Assert.NotNull(context);
            GoodMySqlDbContext gcontext = context as GoodMySqlDbContext;
            Assert.NotNull(gcontext);
        }
    }
}
#endif