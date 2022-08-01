// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test;

public class RabbitMQTypeLocatorTest
{
    [Fact]
    public void Property_Can_Locate_ConnectionTypes()
    {
        // arrange -- handled by including a compatible RabbitMQ NuGet package
        var interfaceType = RabbitMQTypeLocator.ConnectionFactoryInterface;
        var implementationType = RabbitMQTypeLocator.ConnectionFactory;
        var connectionType = RabbitMQTypeLocator.ConnectionInterface;

        Assert.NotNull(interfaceType);
        Assert.NotNull(implementationType);
        Assert.NotNull(connectionType);
    }

    [Fact]
    public void Throws_When_ConnectionType_NotFound()
    {
        var types = RabbitMQTypeLocator.ConnectionInterfaceTypeNames;
        RabbitMQTypeLocator.ConnectionInterfaceTypeNames = new[] { "something-Wrong" };

        var exception = Assert.Throws<TypeLoadException>(() => RabbitMQTypeLocator.ConnectionFactoryInterface);

        Assert.Equal("Unable to find IConnectionFactory, are you missing the RabbitMQ.Client assembly?", exception.Message);

        // reset
        RabbitMQTypeLocator.ConnectionInterfaceTypeNames = types;
    }
}
