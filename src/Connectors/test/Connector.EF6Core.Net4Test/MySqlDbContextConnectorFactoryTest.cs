﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET461
using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.MySql.EF6.Test
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