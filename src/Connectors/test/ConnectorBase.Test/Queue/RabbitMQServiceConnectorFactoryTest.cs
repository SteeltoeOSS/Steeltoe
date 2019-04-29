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

using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ.Test
{
    public class RabbitMQServiceConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            RabbitMQProviderConnectorOptions config = null;
            RabbitMQServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMQProviderConnectorFactory(si, config, typeof(ConnectionFactory)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsRabbitMQConnection()
        {
            RabbitMQProviderConnectorOptions config = new RabbitMQProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 5672,
                Password = "password",
                Username = "username",
                VirtualHost = "vhost"
            };
            RabbitMQServiceInfo si = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");
            var factory = new RabbitMQProviderConnectorFactory(si, config, typeof(ConnectionFactory));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
