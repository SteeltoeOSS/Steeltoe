//
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
//

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Steeltoe.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

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
            var appsettings = @"
{
   'redis': {
        'client': {
            'host': '127.0.0.1',
            'port': 1234,
            'password': 'password',
            'abortOnConnectFail': false
        }
   }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
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
            var env1 = @"
{
      'limits': {
        'fds': 16384,
        'mem': 1024,
        'disk': 1024
      },
      'application_name': 'spring-cloud-broker',
      'application_uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'name': 'spring-cloud-broker',
      'space_name': 'p-spring-cloud-services',
      'space_id': '65b73473-94cc-4640-b462-7ad52838b4ae',
      'uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'users': null,
      'version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_id': '798c2495-fe75-49b1-88da-b81197f2bf06'
    }
}";
            var env2 = @"
{
      'p-redis': [
        {
            'credentials': {
                'host': '192.168.0.103',
                'password': '133de7c8-9f3a-4df1-8a10-676ba7ddaa10',
                'port': 60287
            },
          'syslog_drain_url': null,
          'label': 'p-redis',
          'provider': null,
          'plan': 'shared-vm',
          'name': 'myRedisService1',
          'tags': [
            'pivotal',
            'redis'
          ]
        }, 
        {
            'credentials': {
                'host': '192.168.0.103',
                'password': '133de7c8-9f3a-4df1-8a10-676ba7ddaa10',
                'port': 60287
            },
          'syslog_drain_url': null,
          'label': 'p-redis',
          'provider': null,
          'plan': 'shared-vm',
          'name': 'myRedisService2',
          'tags': [
            'pivotal',
            'redis'
          ]
        } 
      ]
}
";
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

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
