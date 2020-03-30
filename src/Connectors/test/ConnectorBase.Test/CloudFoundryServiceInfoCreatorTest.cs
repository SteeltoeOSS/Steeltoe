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
using Steeltoe.Connector.CloudFoundry;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.Test
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
            Assert.Equal(13, inst.Factories.Count);
        }

        [Fact]
        public void BuildServiceInfos_NoCloudFoundryServices_BuildsExpected()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);

            var builder = new ConfigurationBuilder();
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
            var environment2 = @"
                {
                    ""p-mysql"": [{
                        ""credentials"": {
                            ""hostname"": ""192.168.0.90"",
                            ""port"": 3306,
                            ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                            ""username"": ""Dd6O1BPXUHdrmzbP"",
                            ""password"": ""7E1LxXnlH2hhlPVt"",
                            ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                            ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-mysql"",
                        ""provider"": null,
                        ""plan"": ""100mb-dev"",
                        ""name"": ""spring-cloud-broker-db"",
                        ""tags"": [
                            ""mysql"",
                            ""relational""
                        ]
                    }],
                    ""p-rabbitmq"": [{
                        ""credentials"": {
                            ""http_api_uris"": [""https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/""],
                            ""ssl"": false,
                            ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/03c7a684-6ff1-4bd0-ad45-d10374ffb2af/l5oq2q0unl35s6urfsuib0jvpo"",
                            ""password"": ""l5oq2q0unl35s6urfsuib0jvpo"",
                            ""protocols"": {
                                ""management"": {
                                    ""path"": ""/api/"",
                                    ""ssl"": false,
                                    ""hosts"": [""192.168.0.81""],
                                    ""password"": ""l5oq2q0unl35s6urfsuib0jvpo"",
                                    ""username"": ""03c7a684-6ff1-4bd0-ad45-d10374ffb2af"",
                                    ""port"": 15672,
                                    ""host"": ""192.168.0.81"",
                                    ""uri"": ""https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:15672/api/"",
                                    ""uris"": [""https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:15672/api/""]
                                },
                                ""amqp"": {
                                    ""vhost"": ""fb03d693-91fe-4dc5-8203-ff7a6390df66"",
                                    ""username"": ""03c7a684-6ff1-4bd0-ad45-d10374ffb2af"",
                                    ""password"": ""l5oq2q0unl35s6urfsuib0jvpo"",
                                    ""port"": 5672,
                                    ""host"": ""192.168.0.81"",
                                    ""hosts"": [""192.168.0.81""],
                                    ""ssl"": false,
                                    ""uri"": ""amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66"",
                                    ""uris"": [""amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81:5672/fb03d693-91fe-4dc5-8203-ff7a6390df66""]
                                }
                            },
                            ""username"": ""03c7a684-6ff1-4bd0-ad45-d10374ffb2af"",
                            ""hostname"": ""192.168.0.81"",
                            ""hostnames"": [""192.168.0.81""],
                            ""vhost"": ""fb03d693-91fe-4dc5-8203-ff7a6390df66"",
                            ""http_api_uri"": ""https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/"",
                            ""uri"": ""amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66"",
                            ""uris"": [""amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66""]
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-rabbitmq"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""spring-cloud-broker-rmq"",
                        ""tags"": [
                            ""rabbitmq"",
                            ""messaging"",
                            ""message-queue"",
                            ""amqp"",
                            ""stomp"",
                            ""mqtt"",
                            ""pivotal""
                        ]
                    }]
                }";

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment2);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(creator.ServiceInfos);
            Assert.Equal(2, creator.ServiceInfos.Count);
        }

        [Fact]
        public void GetServiceInfo_NoVCAPs_ReturnsExpected()
        {
            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);
            var result = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result);
            Assert.Empty(result);

            var result2 = creator.GetServiceInfos(typeof(MySqlServiceInfo));
            Assert.NotNull(result2);
            Assert.Empty(result2);

            var result3 = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result3);
            Assert.Empty(result3);

            var result4 = creator.GetServiceInfos(typeof(RedisServiceInfo));
            Assert.NotNull(result4);
            Assert.Empty(result4);

            var result5 = creator.GetServiceInfo<MySqlServiceInfo>("foobar-db2");
            Assert.Null(result5);

            var result6 = creator.GetServiceInfo<MySqlServiceInfo>("spring-cloud-broker-db2");
            Assert.Null(result6);

            var result7 = creator.GetServiceInfo("spring-cloud-broker-db2");
            Assert.Null(result7);

            var result8 = creator.GetServiceInfo<RedisServiceInfo>("spring-cloud-broker-db2");
            Assert.Null(result8);
        }

        [Fact]
        public void GetServiceInfosType_WithVCAPs_ReturnsExpected()
        {
            // Arrange
            var environment2 = @"
                {
                    ""p-mysql"": [{
                        ""credentials"": {
                            ""hostname"": ""192.168.0.90"",
                            ""port"": 3306,
                            ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                            ""username"": ""Dd6O1BPXUHdrmzbP"",
                            ""password"": ""7E1LxXnlH2hhlPVt"",
                            ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                            ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-mysql"",
                        ""provider"": null,
                        ""plan"": ""100mb-dev"",
                        ""name"": ""spring-cloud-broker-db"",
                        ""tags"": [
                            ""mysql"",
                            ""relational""
                        ]
                    },
                    {
                        ""credentials"": {
                            ""hostname"": ""192.168.0.90"",
                            ""port"": 3306,
                            ""name"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355"",
                            ""username"": ""Dd6O1BPXUHdrmzbP"",
                            ""password"": ""7E1LxXnlH2hhlPVt"",
                            ""uri"": ""mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true"",
                            ""jdbcUrl"": ""jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-mysql"",
                        ""provider"": null,
                        ""plan"": ""100mb-dev"",
                        ""name"": ""spring-cloud-broker-db2"",
                        ""tags"": [
                            ""mysql"",
                            ""relational""
                        ]
                    }]
                }";

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment2);

            var builder = new ConfigurationBuilder();
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
            Assert.Empty(result3);

            var result4 = creator.GetServiceInfos(typeof(RedisServiceInfo));
            Assert.NotNull(result4);
            Assert.Empty(result4);

            var result5 = creator.GetServiceInfo<MySqlServiceInfo>("foobar-db2");
            Assert.Null(result5);

            var result6 = creator.GetServiceInfo<MySqlServiceInfo>("spring-cloud-broker-db2");
            Assert.NotNull(result6);
            Assert.True(result6 is MySqlServiceInfo);

            var result7 = creator.GetServiceInfo("spring-cloud-broker-db2");
            Assert.NotNull(result7);
            Assert.True(result7 is MySqlServiceInfo);

            var result8 = creator.GetServiceInfo<RedisServiceInfo>("spring-cloud-broker-db2");
            Assert.Null(result8);
        }

        [Fact]
        public void BuildServiceInfos_WithCloudFoundryServices_WithInvalidURIInMonogoBinding_BuildsExpected()
        {
            // Arrange
            var environment2 = @"
                {
                    ""p-redis"": [{
                        ""credentials"": {
                            ""host"": ""10.66.32.54"",
                            ""password"": ""4254bd8b-7f83-4a8d-8f38-8206a9d7a9f7"",
                            ""port"": 43887
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-redis"",
                        ""provider"": null,
                        ""plan"": ""shared-vm"",
                        ""name"": ""autosource_redis_cache"",
                        ""tags"": [
                            ""pivotal"",
                            ""redis""
                        ]
                    }],
                    ""mongodb-odb"": [{
                        ""credentials"": {
                            ""database"": ""foo"",
                            ""password"": ""bar"",
                            ""servers"": [
                                ""10.66.105.19:28000"",
                                ""10.66.105.39:28000"",
                                ""10.66.105.20:28000""
                            ],
                            ""uri"": ""mongodb://foo:bar@10.66.105.19:28000,10.66.105.39:28000,10.66.105.20:28000/foo?authSource=admin"",
                            ""username"": ""foo""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""mongodb-odb"",
                        ""provider"": null,
                        ""plan"": ""replica_set"",
                        ""name"": ""autosource_jobs_vehicle"",
                        ""tags"": [""mongodb""]
                    },
                    {
                        ""credentials"": {
                            ""database"": ""foo1"",
                            ""password"": ""bar1"",
                            ""servers"": [
                                ""10.66.105.42:28000"",
                                ""10.66.105.45:28000"",
                                ""10.66.105.41:28000""
                            ],
                            ""uri"": ""mongodb://foo1:bar1@10.66.105.42:28000,10.66.105.45:28000,10.66.105.41:28000/foo1?authSource=admin"",
                            ""username"": ""bar1""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""mongodb-odb"",
                        ""provider"": null,
                        ""plan"": ""replica_set"",
                        ""name"": ""autosource_vehicle_service_mongodb"",
                        ""tags"": [""mongodb""]
                    }],
                    ""p-service-registry"": [{
                        ""credentials"": {
                            ""uri"": ""https://eureka-a015d976-af2e-430c-81f6-4f99272ccd24.apps.preprdpcf01.foo.com"",
                            ""client_secret"": ""foo"",
                            ""client_id"": ""p-service-registry-bd61a360-f39a-45f8-b022-bbb"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.sys.preprdpcf01.foo.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""autosource_service_registry"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    }],
                    ""p-config-server"": [{
                        ""credentials"": {
                            ""uri"": ""https://config-86e87517-cf1f-4112-af74-c9c7c957e7df.apps.preprdpcf01.foo.com"",
                            ""client_secret"": ""foo"",
                            ""client_id"": ""p-config-server-4c82e877-1c3c-4029-a7cc-4886ae3f444"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.sys.preprdpcf01.foo.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-config-server"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""autosource_config_server"",
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }],
                    ""user-provided"": [{
                        ""credentials"": {
                            ""uri"": ""10.66.42.50:9001"",
                            ""accesskey"": ""foo"",
                            ""secretkey"": ""bar""
                        },
                        ""syslog_drain_url"": """",
                        ""volume_mounts"": [],
                        ""label"": ""user-provided"",
                        ""name"": ""foo_minio"",
                        ""tags"": []
                    }]
                }";

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment2);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var creator = CloudFoundryServiceInfoCreator.Instance(config);
            Assert.NotNull(creator.ServiceInfos);
            Assert.Equal(4, creator.ServiceInfos.Count);

            var result1 = creator.GetServiceInfos<RedisServiceInfo>();
            Assert.NotNull(result1);
            Assert.Single(result1);

            var redis1 = result1[0];
            Assert.Equal("10.66.32.54", redis1.Host);

            var resutlt2 = creator.GetServiceInfos<MongoDbServiceInfo>();
            Assert.Equal(2, resutlt2.Count);

            var result3 = creator.GetServiceInfos<EurekaServiceInfo>();
            Assert.Single(result3);
            Assert.Equal("eureka-a015d976-af2e-430c-81f6-4f99272ccd24.apps.preprdpcf01.foo.com", result3[0].Host);
        }
    }
}
