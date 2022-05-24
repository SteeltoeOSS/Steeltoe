// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Hystrix.Test
{
    public class HystrixProviderConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            const IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new HystrixProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>
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
            var appsettings = new Dictionary<string, string>
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
