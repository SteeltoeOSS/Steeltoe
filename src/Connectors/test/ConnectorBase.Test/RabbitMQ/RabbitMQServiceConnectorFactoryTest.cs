// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test;

public class RabbitMQServiceConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const RabbitMQProviderConnectorOptions options = null;
        const RabbitMQServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMQProviderConnectorFactory(si, options, typeof(ConnectionFactory)));
        Assert.Contains(nameof(options), ex.Message);
    }

    [Fact]
    public void Create_ReturnsRabbitMQConnection()
    {
        var config = new RabbitMQProviderConnectorOptions
        {
            Server = "localhost",
            Port = 5672,
            Password = "password",
            Username = "username",
            VirtualHost = "vhost"
        };
        var si = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost");
        var factory = new RabbitMQProviderConnectorFactory(si, config, typeof(ConnectionFactory));
        var connection = factory.Create(null);
        Assert.NotNull(connection);
    }
}
