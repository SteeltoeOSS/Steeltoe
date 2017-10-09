//
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
//

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryServiceOptionsTest
    {
        [Fact]
        public void Constructor_WithNoVcapServicesConfiguration()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            var config = builder.Build();

            var options = new CloudFoundryServicesOptions();
            var servSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            servSection.Bind(options);

            Assert.NotNull(options);
            Assert.NotNull(options.Services);
            Assert.Empty(options.Services);
            Assert.Empty(options.ServicesList);
        }

        [Fact]
        public void Constructor_WithSingleServiceConfiguration()
        {
            // Arrange
            var configJson = @"
{ 'vcap': {
    'services' : {
            'p-config-server': [
            {
            'credentials': {
                'access_token_uri': 'https://p-spring-cloud-services.uaa.wise.com/oauth/token',
                'client_id': 'p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef',
                'client_secret': 'e8KF1hXvAnGd',
                'uri': 'http://localhost:8888'
            },
            'label': 'p-config-server',
            'name': 'My Config Server',
            'plan': 'standard',
            'tags': [
                'configuration',
                'spring-cloud'
                ]
            }
            ]
        }
    }
}";
            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryServicesOptions();
            var servSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            servSection.Bind(options);

            Assert.NotNull(options.Services);
            Assert.Single(options.Services);

            Assert.NotNull(options.Services["p-config-server"]);
            Assert.Single(options.Services["p-config-server"]);

            Assert.Equal("p-config-server", options.ServicesList[0].Label);
            Assert.Equal("My Config Server", options.ServicesList[0].Name);
            Assert.Equal("standard", options.ServicesList[0].Plan);

            Assert.NotNull(options.ServicesList[0].Tags);
            Assert.Equal(2, options.ServicesList[0].Tags.Length);
            Assert.Equal("configuration", options.ServicesList[0].Tags[0]);
            Assert.Equal("spring-cloud", options.ServicesList[0].Tags[1]);

            Assert.NotNull(options.ServicesList[0].Credentials);
            Assert.Equal(4, options.ServicesList[0].Credentials.Count);
            Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", options.ServicesList[0].Credentials["access_token_uri"].Value);
            Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", options.ServicesList[0].Credentials["client_id"].Value);
            Assert.Equal("e8KF1hXvAnGd", options.ServicesList[0].Credentials["client_secret"].Value);
            Assert.Equal("http://localhost:8888", options.ServicesList[0].Credentials["uri"].Value);
        }

        [Fact]
        public void Constructor_WithComplexSingleServiceConfiguration()
        {
            // Arrange
            var configJson = @"
{ 'vcap': {
    'services' : {
            'p-rabbitmq': [
            {
            'name': 'rabbitmq',
            'label': 'p-rabbitmq',
            'tags': [
                'rabbitmq',
                'messaging',
                'message-queue',
                'amqp',
                'stomp',
                'mqtt',
                'pivotal'
                ],
            'plan': 'standard',
            'credentials': {
                'http_api_uris': [
                    'https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.testcloud.com/api/'
                ],
                'ssl': false,
                'dashboard_url': 'https://pivotal-rabbitmq.system.testcloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4',
                'password': '3fnpvbqm0djq5jl9fp6fc697f4',
                'protocols': {
                    'management': {
                        'path': '/api/',
                        'ssl': false,
                        'hosts': [
                            '192.168.0.97'
                            ],
                        'password': '3fnpvbqm0djq5jl9fp6fc697f4',
                        'username': '268371bd-07e5-46f3-aec7-d1633ae20bbb',
                        'port': 15672,
                        'host': '192.168.0.97',
                        'uri': 'http://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/',
                        'uris': [
                            'http://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:15672/api/'
                            ]
                    },
                    'amqp': {
                        'vhost': '2260a117-cf28-4725-86dd-37b3b8971052',
                        'username': '268371bd-07e5-46f3-aec7-d1633ae20bbb',
                        'password': '3fnpvbqm0djq5jl9fp6fc697f4',
                        'port': 5672,
                        'host': '192.168.0.97',
                        'hosts': [
                            '192.168.0.97'
                            ],
                        'ssl': false,
                        'uri': 'amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052',
                        'uris': [
                            'amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052'
                            ]
                    }
                },
                'username': '268371bd-07e5-46f3-aec7-d1633ae20bbb',
                'hostname': '192.168.0.97',
                'hostnames': [
                    '192.168.0.97'
                    ],
                'vhost': '2260a117-cf28-4725-86dd-37b3b8971052',
                'http_api_uri': 'https://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@pivotal-rabbitmq.system.testcloud.com/api/',
                'uri': 'amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052',
                'uris': [
                    'amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97/2260a117-cf28-4725-86dd-37b3b8971052'
                    ]
            }
            }
            ]
        }
    }
}";
            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryServicesOptions();
            var servSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            servSection.Bind(options);

            Assert.NotNull(options.Services);
            Assert.Single(options.Services);
            Assert.Equal("p-rabbitmq", options.ServicesList[0].Label);
            Assert.Equal("rabbitmq", options.ServicesList[0].Name);
            Assert.Equal("standard", options.ServicesList[0].Plan);

            Assert.NotNull(options.ServicesList[0].Tags);
            Assert.Equal(7, options.ServicesList[0].Tags.Length);
            Assert.Equal("rabbitmq", options.ServicesList[0].Tags[0]);
            Assert.Equal("pivotal", options.ServicesList[0].Tags[6]);

            Assert.NotNull(options.ServicesList[0].Credentials);
            Assert.Equal(12, options.ServicesList[0].Credentials.Count);
            Assert.Equal("https://pivotal-rabbitmq.system.testcloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4", options.ServicesList[0].Credentials["dashboard_url"].Value);
            Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", options.ServicesList[0].Credentials["username"].Value);
            Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", options.ServicesList[0].Credentials["password"].Value);
            Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", options.ServicesList[0].Credentials["protocols"]["amqp"]["username"].Value);
            Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", options.ServicesList[0].Credentials["protocols"]["amqp"]["password"].Value);
            Assert.Equal("amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052",
                options.ServicesList[0].Credentials["protocols"]["amqp"]["uris"]["0"].Value);

        }
        [Fact]
        public void Constructor_WithMultipleSameServicesConfiguration()
        {
            // Arrange
            var configJson = @"
{ 'vcap': {
    'services' : {
            'p-mysql': [
            {
                'name': 'mySql1',
                'label': 'p-mysql',
                'tags': [
                'mysql',
                'relational'
                ],
                'plan': '100mb-dev',
                'credentials': {
                    'hostname': '192.168.0.97',
                    'port': 3306,
                    'name': 'cf_0f5dda44_e678_4727_993f_30e6d455cc31',
                    'username': '9vD0Mtk3wFFuaaaY',
                    'password': 'Cjn4HsAiKV8sImst',
                    'uri': 'mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true',
                    'jdbcUrl': 'jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst'
                }
            },
            {
                'name': 'mySql2',
                'label': 'p-mysql',
                'tags': [
                'mysql',
                'relational'
                ],
                'plan': '100mb-dev',
                'credentials': {
                    'hostname': '192.168.0.97',
                    'port': 3306,
                    'name': 'cf_0f5dda44_e678_4727_993f_30e6d455cc31',
                    'username': '9vD0Mtk3wFFuaaaY',
                    'password': 'Cjn4HsAiKV8sImst',
                    'uri': 'mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true',
                    'jdbcUrl': 'jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst'
                }
            }
            ]
        }
    }
}";
            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryServicesOptions();
            var servSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            servSection.Bind(options);


            Assert.NotNull(options.Services);
            Assert.Single(options.Services);
            Assert.NotNull(options.Services["p-mysql"]);

            Assert.Equal(2, options.ServicesList.Count);

            Assert.Equal("p-mysql", options.ServicesList[0].Label);
            Assert.Equal("p-mysql", options.ServicesList[1].Label);

            Assert.True(options.ServicesList[0].Name.Equals("mySql1") || options.ServicesList[0].Name.Equals("mySql2"));
            Assert.True(options.ServicesList[1].Name.Equals("mySql1") || options.ServicesList[1].Name.Equals("mySql2"));

            Assert.Equal("192.168.0.97", options.ServicesList[0].Credentials["hostname"].Value);
            Assert.Equal("192.168.0.97", options.ServicesList[1].Credentials["hostname"].Value);
            Assert.Equal("3306", options.ServicesList[0].Credentials["port"].Value);
            Assert.Equal("cf_0f5dda44_e678_4727_993f_30e6d455cc31", options.ServicesList[0].Credentials["name"].Value);
            Assert.Equal("cf_0f5dda44_e678_4727_993f_30e6d455cc31", options.ServicesList[1].Credentials["name"].Value);

        }
    }
}
