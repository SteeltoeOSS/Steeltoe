// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Connector.Hystrix;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Test.Hystrix;

public class HystrixProviderConnectorFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const HystrixProviderConnectorOptions options = null;
        const HystrixRabbitMQServiceInfo si = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new HystrixProviderConnectorFactory(si, options, typeof(ConnectionFactory)));
        Assert.Contains(nameof(options), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ReturnsRabbitMQConnection()
    {
        var options = new HystrixProviderConnectorOptions
        {
            Server = "localhost",
            Port = 5672,
            Password = "password",
            Username = "username",
            VirtualHost = "vhost"
        };

        var si = new HystrixRabbitMQServiceInfo("MyId", "amqp://si_username:si_password@example.com:5672/si_vhost", false);
        var factory = new HystrixProviderConnectorFactory(si, options, typeof(ConnectionFactory));
        object connection = factory.Create(null);
        Assert.NotNull(connection);
    }
}
