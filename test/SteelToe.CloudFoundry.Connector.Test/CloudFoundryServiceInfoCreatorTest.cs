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
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test
{
    public class CloudFoundryServiceInfoCreatorTest
    {
        public CloudFoundryServiceInfoCreatorTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceInfoCreator.Instance(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_ReturnsInstance()
        {
            // Arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // Act and Assert
            var inst = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(inst);
        }

        [Fact]
        public void Constructor_ReturnsSameInstance()
        {
            // Arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // Act and Assert
            var inst = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(inst);
            var inst2 = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.Same(inst, inst2);
        }
        [Fact]
        public void Constructor_ReturnsNewInstance()
        {
            // Arrange
            IConfiguration config = new ConfigurationBuilder().Build();
            IConfiguration config2 = new ConfigurationBuilder().Build();

            // Act and Assert
            var inst = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(inst);

            var inst2 = CloudFoundryServiceInfoCreator.Instance(config2);
            Assert.NotSame(inst, inst2);
        }
        [Fact]
        public void BuildServiceInfoFactories_BuildsExpected()
        {
            // Arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // Act and Assert
            var inst = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(inst);
            Assert.NotNull(inst.Factories);
            Assert.Equal(10, inst.Factories.Count);

        }
        [Fact]
        public void BuildServiceInfos_NoCloudFoundryServices_BuildsExpected()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var creator = CloudFoundryServiceInfoCreator.Instance(config);

            Assert.NotNull(creator.ServiceInfos);
            Assert.Equal(0, creator.ServiceInfos.Count);
        }

        [Fact]
        public void BuildServiceInfos_WithCloudFoundryServices_BuildsExpected()
        {
            // Arrange
            var environment1 = @"
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
            var environment2 = @"
{
      'p-mysql': [
        {
            'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
                },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db',
          'tags': [
            'mysql',
            'relational'
          ]
    }
      ],
      'p-rabbitmq': [
        {
          'credentials': {
            'http_api_uris': [
              'https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/'
            ],
            'ssl': false,
            'dashboard_url': 'https://pivotal-rabbitmq.system.testcloud.com/#/login/03c7a684-6ff1-4bd0-ad45-d10374ffb2af/l5oq2q0unl35s6urfsuib0jvpo',
            'password': 'l5oq2q0unl35s6urfsuib0jvpo',
            'protocols': {
              'management': {
                'path': '/api/',
                'ssl': false,
                'hosts': [
                  '192.168.0.81'
                ],
                'password': 'l5oq2q0unl35s6urfsuib0jvpo',
                'username': '03c7a684-6ff1-4bd0-ad45-d10374ffb2af',
                'port': 15672,
                'host': '192.168.0.81',
                'uri': 'http://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:15672/api/',
                'uris': [
                  'http://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:15672/api/'
                ]
},
              'amqp': {
                'vhost': 'fb03d693-91fe-4dc5-8203-ff7a6390df66',
                'username': '03c7a684-6ff1-4bd0-ad45-d10374ffb2af',
                'password': 'l5oq2q0unl35s6urfsuib0jvpo',
                'port': 5672,
                'host': '192.168.0.81',
                'hosts': [
                  '192.168.0.81'
                ],
                'ssl': false,
                'uri': 'amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66',
                'uris': [
                  'amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66'
                ]
              }
            },
            'username': '03c7a684-6ff1-4bd0-ad45-d10374ffb2af',
            'hostname': '192.168.0.81',
            'hostnames': [
              '192.168.0.81'
            ],
            'vhost': 'fb03d693-91fe-4dc5-8203-ff7a6390df66',
            'http_api_uri': 'https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/',
            'uri': 'amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66',
            'uris': [
              'amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66'
            ]
          },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'spring-cloud-broker-rmq',
          'tags': [
            'rabbitmq',
            'messaging',
            'message-queue',
            'amqp',
            'stomp',
            'mqtt',
            'pivotal'
          ]
        }
      ]
    }
  }";

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(creator.ServiceInfos);
            Assert.Equal(2, creator.ServiceInfos.Count);
        }

        [Fact]
        public void GetServiceInfo_NoVCAPs_ReturnsExpected()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);
            var result = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result);
            Assert.Equal(0, result.Count);

            var result2 = creator.GetServiceInfos(typeof(MySqlServiceInfo));
            Assert.NotNull(result2);
            Assert.Equal(0, result2.Count);

            var result3 = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result3);
            Assert.Equal(0, result3.Count);

            var result4 = creator.GetServiceInfos(typeof(RedisServiceInfo));
            Assert.NotNull(result4);
            Assert.Equal(0, result4.Count);

            var result5 = creator.GetServiceInfo<MySqlServiceInfo>("foobar-db2");
            Assert.Null(result5);

            var result6 = creator.GetServiceInfo<MySqlServiceInfo>("spring-cloud-broker-db2");
            Assert.Null(result6);

            var result7 = creator.GetServiceInfo("spring-cloud-broker-db2");
            Assert.Null(result7);

            var result8 = creator.GetServiceInfo<RedisServiceInfo>("spring-cloud-broker-db2");
            Assert.Null(result8);
        }

        public void GetServiceInfosType_WithVCAPs_ReturnsExpected()
        {
            // Arrange
            var environment1 = @"
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
            var environment2 = @"
{
      'p-mysql': [
        {
            'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
                },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db',
          'tags': [
            'mysql',
            'relational'
          ]
        },
        {
            'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
                },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db2',
          'tags': [
            'mysql',
            'relational'
          ]
        }
      ]
  }";

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);

            var result = creator.GetServiceInfos<MySqlServiceInfo>();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.True(result[0] is MySqlServiceInfo);
            Assert.True(result[1] is MySqlServiceInfo);

            var result2 = creator.GetServiceInfos(typeof(MySqlServiceInfo));
            Assert.NotNull(result2);
            Assert.Equal(2, result2.Count);
            Assert.True(result2[0] is MySqlServiceInfo);
            Assert.True(result2[1] is MySqlServiceInfo);

            var result3 = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result3);
            Assert.Equal(0, result3.Count);

            var result4 = creator.GetServiceInfos(typeof(RedisServiceInfo));
            Assert.NotNull(result4);
            Assert.Equal(0, result4.Count);

            var result5 = creator.GetServiceInfo<MySqlServiceInfo>("foobar-db2");
            Assert.Null(result5);

            var result6 = creator.GetServiceInfo<MySqlServiceInfo>("spring-cloud-broker-db2");
            Assert.NotNull(result6);
            Assert.True(result6 is MySqlServiceInfo);

            var result7 = creator.GetServiceInfo("spring-cloud-broker-db2");
            Assert.NotNull(result7);
            Assert.True(result7 is MySqlServiceInfo);

            var result8 = creator.GetServiceInfo<RedisServiceInfo>( "spring-cloud-broker-db2");
            Assert.Null(result8);
        }
    }
}
