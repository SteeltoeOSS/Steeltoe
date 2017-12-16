// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    public class RedisCacheConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RedisCacheConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:host"] = "localhost",
                ["redis:client:port"] = "1234",
                ["redis:client:password"] = "password",
                ["redis:client:instancename"] = "instanceid",
                ["redis:client:allowAdmin"] = "true",
                ["redis:client:clientName"] = "foobar",
                ["redis:client:connectRetry"] = "100",
                ["redis:client:connectTimeout"] = "100",
                ["redis:client:abortOnConnectFail"] = "true",
                ["redis:client:keepAlive"] = "100",
                ["redis:client:resolveDns"] = "true",
                ["redis:client:serviceName"] = "foobar",
                ["redis:client:ssl"] = "true",
                ["redis:client:sslHost"] = "foobar",
                ["redis:client:writeBuffer"] = "100",
                ["redis:client:tieBreaker"] = "foobar"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new RedisCacheConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Host);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("instanceid", sconfig.InstanceName);

            Assert.True(sconfig.AllowAdmin);
            Assert.Equal("foobar", sconfig.ClientName);
            Assert.Equal(100, sconfig.ConnectRetry);
            Assert.Equal(100, sconfig.ConnectTimeout);
            Assert.True(sconfig.AbortOnConnectFail);
            Assert.Equal(100, sconfig.KeepAlive);
            Assert.True(sconfig.ResolveDns);
            Assert.Equal("foobar", sconfig.ServiceName);
            Assert.True(sconfig.Ssl);
            Assert.Equal("foobar", sconfig.SslHost);
            Assert.Equal("foobar", sconfig.TieBreaker);
            Assert.Equal(100, sconfig.WriteBuffer);

            Assert.Null(sconfig.ConnectionString);
    }

        [Fact]
        public void ConnectionString_Returned_AsConfigured()
        {
            // arrange
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var sconfig = new RedisCacheConnectorOptions(config);

            // assert
            Assert.Equal(appsettings["redis:client:ConnectionString"], sconfig.ToString());
        }

        [Fact]
        public void ConnectionString_Overridden_By_CloudFoundryConfig()
        {
            // arrange
            // simulate an appsettings file
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:ConnectionString"] = "Server=fake;Database=test;Uid=steeltoe;Pwd=password;"
            };

            // add environment variables as Cloud Foundry would
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVCAP);

            // add settings to config
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            // act
            var sconfig = new RedisCacheConnectorOptions(config);

            // assert
            Assert.NotEqual(appsettings["redis:client:ConnectionString"], sconfig.ToString());
        }
    }
}
