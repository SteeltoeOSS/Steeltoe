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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    public class RedisCacheConfigurationExtensionsTest
    {
        public RedisCacheConfigurationExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_ThrowsIfConfigurationNull()
        {
            // Arrange
            IConfigurationRoot config = null;
            IConfigurationRoot connectorConfiguration = new ConfigurationBuilder().Build();
            RedisCacheConnectorOptions connectorOptions = new RedisCacheConnectorOptions();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, "foobar"));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorConfiguration, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorOptions, "foobar"));
            Assert.Contains(nameof(config), ex3.Message);
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_ThrowsIfConnectorConfigurationNull()
        {
            // Arrange
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            IConfigurationRoot connectorConfiguration = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorConfiguration, "foobar"));
            Assert.Contains(nameof(connectorConfiguration), ex.Message);
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_ThrowsIfConnectorOptionsNull()
        {
            // Arrange
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            RedisCacheConnectorOptions connectorOptions = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorOptions, "foobar"));
            Assert.Contains(nameof(connectorOptions), ex.Message);
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            RedisCacheConnectorOptions connectorOptions = new RedisCacheConnectorOptions();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, "foobar"));
            Assert.Contains("foobar", ex.Message);

            var ex2 = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, config, "foobar"));
            Assert.Contains("foobar", ex2.Message);

            var ex3 = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorOptions, "foobar"));
            Assert.Contains("foobar", ex3.Message);
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_NoVCAPs_CreatesFactory()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:host"] = "127.0.0.1",
                ["redis:client:port"] = "1234",
                ["redis:client:password"] = "password",
                ["redis:client:abortOnConnectFail"] = "false"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var connectorOptions = new RedisCacheConnectorOptions(config);

            // Act and Assert
            Assert.NotNull(RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config));
            Assert.NotNull(RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(new ConfigurationBuilder().Build(), config));
            Assert.NotNull(RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(new ConfigurationBuilder().Build(), connectorOptions));
        }

        [Fact]
        public void CreateRedisServiceConnectorFactory_MultipleRedisServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.TwoServerVCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            RedisCacheConnectorOptions connectorOptions = new RedisCacheConnectorOptions();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config));
            Assert.Contains("Multiple", ex.Message);

            var ex2 = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, config));
            Assert.Contains("Multiple", ex2.Message);

            var ex3 = Assert.Throws<ConnectorException>(() => RedisCacheConfigurationExtensions.CreateRedisServiceConnectorFactory(config, connectorOptions));
            Assert.Contains("Multiple", ex3.Message);
        }
    }
}
