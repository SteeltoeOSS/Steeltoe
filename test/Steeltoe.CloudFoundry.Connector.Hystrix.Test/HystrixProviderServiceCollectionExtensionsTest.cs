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
using RabbitMQ.Client;
using Steeltoe.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Hystrix.Test
{
    public class HystrixProviderServiceCollectionExtensionsTest
    {
        public HystrixProviderServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddHystrixConnection_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddHystrixConnection_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddHystrixConnection_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddHystrixConnection_NoVCAPs_AddsConfiguredConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config);

            var service = services.BuildServiceProvider().GetService<HystrixConnectionFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddHystrixConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex =
                Assert.Throws<ConnectorException>(
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddHystrixConnection_MultipleHystrixServices_ThrowsConnectorException()
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
      'p-circuit-breaker-dashboard': [
    {
        'credentials': {
            'stream': 'https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com',
            'amqp': {
                        'http_api_uris': [
                          'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/'
              ],
              'ssl': false,
              'dashboard_url': 'https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk',
              'password': '1clgf5ipeop36437dmr2em4duk',
              'protocols': {
                'amqp': {
                  'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'port': 5672,
                  'host': '192.168.1.55',
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'ssl': false,
                  'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120',
                  'uris': [
                    'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120'
                  ]
                },
                'management': {
                  'path': '/api/',
                  'ssl': false,
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'port': 15672,
                  'host': '192.168.1.55',
                  'uri': 'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/',
                  'uris': [
                    'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/'
                  ]
                }
              },
              'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
              'hostname': '192.168.1.55',
              'hostnames': [
                '192.168.1.55'
              ],
              'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
              'http_api_uri': 'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/',
              'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120',
              'uris': [
                'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120'
              ]
            },
            'dashboard': 'https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com'
          },
          'syslog_drain_url': null,
          'volume_mounts': [],
          'label': 'p-circuit-breaker-dashboard',
          'provider': null,
          'plan': 'standard',
          'name': 'myHystrixService1',
          'tags': [
            'circuit-breaker',
            'hystrix-amqp',
            'spring-cloud'
          ]
    },
    {
        'credentials': {
            'stream': 'https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com',
            'amqp': {
                'http_api_uris': [
                    'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/'
              ],
              'ssl': false,
              'dashboard_url': 'https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk',
              'password': '1clgf5ipeop36437dmr2em4duk',
              'protocols': {
                'amqp': {
                  'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'port': 5672,
                  'host': '192.168.1.55',
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'ssl': false,
                  'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120',
                  'uris': [
                    'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120'
                  ]
                },
                'management': {
                  'path': '/api/',
                  'ssl': false,
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'port': 15672,
                  'host': '192.168.1.55',
                  'uri': 'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/',
                  'uris': [
                    'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/'
                  ]
                }
              },
              'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
              'hostname': '192.168.1.55',
              'hostnames': [
                '192.168.1.55'
              ],
              'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
              'http_api_uri': 'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/',
              'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120',
              'uris': [
                'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120'
              ]
            },
            'dashboard': 'https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com'
          },
          'syslog_drain_url': null,
          'volume_mounts': [],
          'label': 'p-circuit-breaker-dashboard',
          'provider': null,
          'plan': 'standard',
          'name': 'myHystrixService2',
          'tags': [
            'circuit-breaker',
            'hystrix-amqp',
            'spring-cloud'
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
                    () => HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddHystrixConnection_WithVCAPs_AddsHystrixConnectionFactory()
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
      'p-circuit-breaker-dashboard': [
    {
        'credentials': {
            'stream': 'https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com',
            'amqp': {
                        'http_api_uris': [
                          'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/'
              ],
              'ssl': false,
              'dashboard_url': 'https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk',
              'password': '1clgf5ipeop36437dmr2em4duk',
              'protocols': {
                'amqp': {
                  'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'port': 5672,
                  'host': '192.168.1.55',
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'ssl': false,
                  'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120',
                  'uris': [
                    'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120'
                  ]
                },
                'management': {
                  'path': '/api/',
                  'ssl': false,
                  'hosts': [
                    '192.168.1.55'
                  ],
                  'password': '1clgf5ipeop36437dmr2em4duk',
                  'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
                  'port': 15672,
                  'host': '192.168.1.55',
                  'uri': 'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/',
                  'uris': [
                    'http://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/'
                  ]
                }
              },
              'username': 'a0f39f25-28a2-438e-a0e7-6c09d6d34dbd',
              'hostname': '192.168.1.55',
              'hostnames': [
                '192.168.1.55'
              ],
              'vhost': '06f0b204-9f95-4829-a662-844d3c3d6120',
              'http_api_uri': 'https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/',
              'uri': 'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120',
              'uris': [
                'amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120'
              ]
            },
            'dashboard': 'https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com'
          },
          'syslog_drain_url': null,
          'volume_mounts': [],
          'label': 'p-circuit-breaker-dashboard',
          'provider': null,
          'plan': 'standard',
          'name': 'myHystrixService',
          'tags': [
            'circuit-breaker',
            'hystrix-amqp',
            'spring-cloud'
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
            HystrixProviderServiceCollectionExtensions.AddHystrixConnection(services, config);

            var hystrixService = services.BuildServiceProvider().GetService<HystrixConnectionFactory>();
            Assert.NotNull(hystrixService);
            var service = hystrixService.ConnectionFactory as ConnectionFactory;
            Assert.NotNull(service);
            Assert.Equal("06f0b204-9f95-4829-a662-844d3c3d6120", service.VirtualHost);
            Assert.Equal(5672, service.Port);
            Assert.Equal("192.168.1.55", service.HostName);
            Assert.Equal("a0f39f25-28a2-438e-a0e7-6c09d6d34dbd", service.UserName);
            Assert.Equal("1clgf5ipeop36437dmr2em4duk", service.Password);
        }
    }
}
