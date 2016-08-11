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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SteelToe.Extensions.Configuration;
using System;
using RabbitMQ.Client;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Rabbit.Test
{
    public class RabbitServiceCollectionExtensionsTest
    {
        public RabbitServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddRabbitConnection_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);

        }

        [Fact]
        public void AddRabbitConnection_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

        }

        [Fact]
        public void AddRabbitConnection_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);

        }

        [Fact]
        public void AddRabbitConnection_NoVCAPs_AddsConfiguredConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config);

            var service = services.BuildServiceProvider().GetService<ConnectionFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddRabbitConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex =
                Assert.Throws<ConnectorException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);

        }

        [Fact]
        public void AddRabbitConnection_MultipleRabbitServices_ThrowsConnectorException()
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
      'p-rabbitmq': [
        {
            'credentials': {
                'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
            },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitService1',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        }, 
        {
            'credentials': {
                'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
            },
          'syslog_drain_url': null,
          'label': 'p-Rabbit',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitService2',
          'tags': [
            'rabbitmq',
            'amqp'
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

            // Act and Assert
            var ex =
                Assert.Throws<ConnectorException>(
                    () => RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddRabbitConnection_WithVCAPs_AddsRabbitConnection()
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
      'p-rabbitmq': [
        {
          'credentials': {
            'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
          },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitService',
          'tags': [
            'rabbitmq',
            'amqp'
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

            // Act and Assert
            RabbitProviderServiceCollectionExtensions.AddRabbitConnection(services, config);

            var service = services.BuildServiceProvider().GetService<ConnectionFactory>();
            Assert.NotNull(service);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", service.VirtualHost);
            Assert.Equal(3306, service.Port);
            Assert.Equal("192.168.0.90", service.HostName);
            Assert.Equal("Dd6O1BPXUHdrmzbP", service.UserName);
            Assert.Equal("7E1LxXnlH2hhlPVt", service.Password);
        }
    }
}
