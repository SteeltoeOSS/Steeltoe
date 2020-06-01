// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ.Test
{
    public class RabbitMQTypeLocatorTest
    {
        [Fact]
        public void Property_Can_Locate_ConnectionTypes()
        {
            // arrange -- handled by including a compatible RabbitMQ NuGet package

            // act
            var interfaceType = RabbitMQTypeLocator.IConnectionFactory;
            var implementationType = RabbitMQTypeLocator.ConnectionFactory;
            var connectionType = RabbitMQTypeLocator.IConnection;

            // assert
            Assert.NotNull(interfaceType);
            Assert.NotNull(implementationType);
            Assert.NotNull(connectionType);
        }

        [Fact]
        public void Throws_When_ConnectionType_NotFound()
        {
            // arrange
            var types = RabbitMQTypeLocator.ConnectionInterfaceTypeNames;
            RabbitMQTypeLocator.ConnectionInterfaceTypeNames = new string[] { "something-Wrong" };

            // act
            var exception = Assert.Throws<ConnectorException>(() => RabbitMQTypeLocator.IConnectionFactory);

            // assert
            Assert.Equal("Unable to find IConnectionFactory, are you missing the RabbitMQ.Client assembly?", exception.Message);

            // reset
            RabbitMQTypeLocator.ConnectionInterfaceTypeNames = types;
        }
    }
}
