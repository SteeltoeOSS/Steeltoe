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

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Rabbit.Test
{
    public class RabbitConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RabbitProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
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

            var sconfig = new RabbitProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.Uri);
            Assert.True(sconfig.SslEnabled);
            Assert.Equal(RabbitProviderConnectorOptions.Default_SSLPort, sconfig.SslPort);
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

            var sconfig = new RabbitProviderConnectorOptions(config);
            string result = sconfig.ToString();
            Assert.Equal("amqps://username:password@localhost:5671/foobar", result);
        }
    }
}
