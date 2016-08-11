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

using System;
using SteelToe.CloudFoundry.Connector.Services;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Rabbit.Test
{
    public class RabbitServiceConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            RabbitProviderConnectorOptions config = null;
            RabbitServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RabbitProviderConnectorFactory(si, config));
            Assert.Contains(nameof(config), ex.Message);

        }
        [Fact]
        public void Create_ReturnsRabbitConnection()
        {
            RabbitProviderConnectorOptions config = new RabbitProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 5672,
                Password = "password",
                Username = "username",
                VirtualHost = "vhost"
                
            };
            RabbitServiceInfo si = new RabbitServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");
            var factory = new RabbitProviderConnectorFactory(si, config);
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
