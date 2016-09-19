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

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    public class MySqlProviderConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            MySqlProviderConnectorOptions config = null;
            MySqlServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MySqlProviderConnectorFactory(si, config));
            Assert.Contains(nameof(config), ex.Message);

        }
        [Fact]
        public void Create_ReturnsMySqlConnection()
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
            var factory = new MySqlProviderConnectorFactory(si, config);
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
