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

using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Hystrix.Test
{
    public class HystrixProviderConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            HystrixProviderConnectorOptions config = null;
            HystrixRabbitServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new HystrixProviderConnectorFactory(si, config, typeof(ConnectionFactory)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsRabbitConnection()
        {
            HystrixProviderConnectorOptions config = new HystrixProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 5672,
                Password = "password",
                Username = "username",
                VirtualHost = "vhost"
            };
            HystrixRabbitServiceInfo si = new HystrixRabbitServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost", false);
            var factory = new HystrixProviderConnectorFactory(si, config, typeof(ConnectionFactory));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
