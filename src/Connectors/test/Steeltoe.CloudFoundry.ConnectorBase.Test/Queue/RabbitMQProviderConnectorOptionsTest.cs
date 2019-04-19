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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ.Test
{
    public class RabbitMQProviderConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RabbitMQProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_Binds_Rabbit_Values()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["rabbit:client:server"] = "localhost",
                ["rabbit:client:port"] = "1234",
                ["rabbit:client:password"] = "password",
                ["rabbit:client:username"] = "username",
                ["rabbit:client:sslEnabled"] = "true"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RabbitMQProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.Uri);
            Assert.True(sconfig.SslEnabled);
            Assert.Equal(RabbitMQProviderConnectorOptions.Default_SSLPort, sconfig.SslPort);
        }

        [Fact]
        public void Constructor_Binds_RabbitMQ_Values()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["rabbitmq:client:server"] = "localhost",
                ["rabbitmq:client:port"] = "1234",
                ["rabbitmq:client:password"] = "password",
                ["rabbitmq:client:username"] = "username",
                ["rabbitmq:client:sslEnabled"] = "true"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RabbitMQProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.Uri);
            Assert.True(sconfig.SslEnabled);
            Assert.Equal(RabbitMQProviderConnectorOptions.Default_SSLPort, sconfig.SslPort);
        }

        [Fact]
        public void ToString_ReturnsValid()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["rabbit:client:server"] = "localhost",
                ["rabbit:client:port"] = "1234",
                ["rabbit:client:password"] = "password",
                ["rabbit:client:username"] = "username",
                ["rabbit:client:virtualHost"] = "foobar",
                ["rabbit:client:sslEnabled"] = "true"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RabbitMQProviderConnectorOptions(config);
            string result = sconfig.ToString();
            Assert.Equal("amqps://username:password@localhost:5671/foobar", result);
        }
    }
}
