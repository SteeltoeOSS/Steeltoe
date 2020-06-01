// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            HystrixRabbitMQServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new HystrixProviderConnectorFactory(si, config, typeof(ConnectionFactory)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsRabbitMQConnection()
        {
            HystrixProviderConnectorOptions config = new HystrixProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 5672,
                Password = "password",
                Username = "username",
                VirtualHost = "vhost"
            };
            HystrixRabbitMQServiceInfo si = new HystrixRabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost", false);
            var factory = new HystrixProviderConnectorFactory(si, config, typeof(ConnectionFactory));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
