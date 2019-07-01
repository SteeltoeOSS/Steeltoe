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
