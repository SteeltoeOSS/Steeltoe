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

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Data.Entity;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EF6.Test
{
    public class SqlServerDbContextConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfTypeNull()
        {
            // Arrange
            SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions();
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
            SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions();
            SqlServerServiceInfo si = null;
            Type dbContextType = typeof(BadDbContext);

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => new SqlServerDbContextConnectorFactory(si, config, dbContextType).Create(null));
            Assert.Contains("BadDbContext", ex.Message);
        }

        // [Fact]
        // public void FindConstructor_FindsCorrectConstructor()
        // {
        //     // Arrange
        //     SqlServerDbContextConnectorFactory factory = new SqlServerDbContextConnectorFactory();
        //     var info = factory.FindConstructor(typeof(GoodDbContext));
        //     Assert.NotNull(info);
        //     Assert.Equal(1, info.GetParameters().Length);
        //     Assert.Equal(typeof(string), info.GetParameters()[0].ParameterType);

        // }
        [Fact]
        public void Create_ReturnsDbContext()
        {
            SqlServerProviderConnectorOptions config = new SqlServerProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1433,
                Password = "password",
                Username = "username",
                Database = "database"
            };
            SqlServerServiceInfo si = new SqlServerServiceInfo("MyId", "SqlServer://192.168.0.90:1433/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
            var factory = new SqlServerDbContextConnectorFactory(si, config, typeof(GoodDbContext));
            var context = factory.Create(null);
            Assert.NotNull(context);
            GoodDbContext gcontext = context as GoodDbContext;
            Assert.NotNull(gcontext);
        }
    }

    public class BadDbContext : DbContext
    {
    }

    public class GoodDbContext : DbContext
    {
        public GoodDbContext(string str)
        {
        }
    }
}
