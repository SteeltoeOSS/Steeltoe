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

namespace Steeltoe.CloudFoundry.Connector.Hystrix.Test
{
    public class HystrixProviderConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new HystrixProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["hystrix:client:server"] = "localhost",
                ["hystrix:client:port"] = "1234",
                ["hystrix:client:password"] = "password",
                ["hystrix:client:username"] = "username",
                ["hystrix:client:sslEnabled"] = "true"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new HystrixProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Null(sconfig.Uri);
            Assert.True(sconfig.SslEnabled);
            Assert.Equal(HystrixProviderConnectorOptions.Default_SSLPort, sconfig.SslPort);
        }

        [Fact]
        public void ToString_ReturnsValid()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["hystrix:client:server"] = "localhost",
                ["hystrix:client:port"] = "1234",
                ["hystrix:client:password"] = "password",
                ["hystrix:client:username"] = "username",
                ["hystrix:client:virtualHost"] = "foobar",
                ["hystrix:client:sslEnabled"] = "true"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new HystrixProviderConnectorOptions(config);
            var result = sconfig.ToString();
            Assert.Equal("amqps://username:password@localhost:5671/foobar", result);
        }
    }
}
