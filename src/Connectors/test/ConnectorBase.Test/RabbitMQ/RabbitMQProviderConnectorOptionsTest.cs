// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test
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

            var configurationBuilder = new ConfigurationBuilder();
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

            var configurationBuilder = new ConfigurationBuilder();
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

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RabbitMQProviderConnectorOptions(config);
            var result = sconfig.ToString();
            Assert.Equal("amqps://username:password@localhost:5671/foobar", result);
        }
    }
}
