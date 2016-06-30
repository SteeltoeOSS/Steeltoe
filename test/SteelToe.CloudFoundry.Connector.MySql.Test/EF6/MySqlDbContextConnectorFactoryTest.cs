//
// Copyright 2015 the original author or authors.
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
//
using SteelToe.CloudFoundry.Connector.Services;
using System;
using System.Data.Entity;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.MySql.EF6.Test
{
    public class MySqlDbContextConnectorFactoryTest
    {

        [Fact]
        public void Constructor_ThrowsIfTypeNull()
        {
        
            // Arrange
            MySqlProviderConfiguration config = new MySqlProviderConfiguration();
            MySqlServiceInfo si = null;
            Type dbContextType = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MySqlDbContextConnectorFactory(si, config, dbContextType));
            Assert.Contains(nameof(dbContextType), ex.Message);

        }
        [Fact]
        public void Constructor_ThrowsIfNoValidConstructorFound()
        {

            // Arrange
            MySqlProviderConfiguration config = new MySqlProviderConfiguration();
            MySqlServiceInfo si = null;
            Type dbContextType = typeof(BadDbContext);

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => new MySqlDbContextConnectorFactory(si, config, dbContextType));
            Assert.Contains("BadDbContext", ex.Message);

        }

        [Fact]
        public void FindConstructor_FindsCorrectConstructor()
        {
            // Arrange
            MySqlDbContextConnectorFactory factory = new MySqlDbContextConnectorFactory();
            var info = factory.FindConstructor(typeof(GoodDbContext));
            Assert.NotNull(info);
            Assert.Equal(1, info.GetParameters().Length);
            Assert.Equal(typeof(string), info.GetParameters()[0].ParameterType);

        }
        [Fact]
        public void Create_ReturnsDbContext()
        {
            MySqlProviderConfiguration config = new MySqlProviderConfiguration()
            {
                Server = "localhost",
                Port = 3306,
                Password = "password",
                Username = "username",
                Database = "database"

            };
            MySqlServiceInfo si = new MySqlServiceInfo("MyId", "mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
            var factory = new MySqlDbContextConnectorFactory(si, config, typeof(GoodDbContext));
            var context = factory.Create(null);
            Assert.NotNull(context);
            GoodDbContext gcontext = context as GoodDbContext;
            Assert.NotNull(gcontext);
      
        }
    }

    class BadDbContext : DbContext
    {

    }
    class GoodDbContext: DbContext
    {
        public GoodDbContext(string str)
        {

        }
    }

}
