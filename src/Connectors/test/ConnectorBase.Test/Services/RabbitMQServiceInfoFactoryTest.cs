﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class RabbitMQServiceInfoFactoryTest
    {
        public RabbitMQServiceInfoFactoryTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var s = CreateRabbitMQService();
            Assert.NotNull(s);

            var factory = new RabbitMQServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsNoLabelNoTagsServiceBinding()
        {
            var s = new Service()
            {
                Name = "rabbitMQService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                }
            };
            var factory = new RabbitMQServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsNoLabelNoTagsSecureUriServiceBinding()
        {
            var s = new Service()
            {
                Name = "rabbitMQService",
                Plan = "free",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("amqps://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                }
            };
            var factory = new RabbitMQServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_WithLabelNoTagsServiceBinding()
        {
            var s = new Service()
            {
                Label = "rabbitmq",
                Name = "myService",
                Plan = "Standard",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "http_api_uri", new Credential("https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/") }
                }
            };
            var factory = new RabbitMQServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-mysql",
                Tags = new string[] { "foobar", "relational" },
                Name = "mySqlService",
                Plan = "100mb-dev",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            var factory = new RabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsHystrixServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-circuit-breaker-dashboard",
                Tags = new string[] { "circuit-breaker", "hystrix-amqp", "spring-cloud" },
                Name = "myHystrixService",
                Plan = "standard",
                Credentials = new Credential()
                {
                    { "stream", new Credential("https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com") },
                    { "dashboard", new Credential("https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com") },
                    {
                        "amqp", new Credential()
                        {
                            { "username", new Credential("a0f39f25-28a2-438e-a0e7-6c09d6d34dbd") },
                            { "password", new Credential("1clgf5ipeop36437dmr2em4duk") },
                            { "uri", new Credential("amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120") },
                            { "ssl", new Credential("false") }
                        }
                    }
                }
            };
            var factory = new RabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var s = CreateRabbitMQService();
            var factory = new RabbitMQServiceInfoFactory();
            var info = factory.Create(s) as RabbitMQServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("spring-cloud-broker-rmq", info.Id);
            Assert.Equal("l5oq2q0unl35s6urfsuib0jvpo", info.Password);
            Assert.Equal("03c7a684-6ff1-4bd0-ad45-d10374ffb2af", info.UserName);
            Assert.Equal("192.168.0.81", info.Host);
            Assert.Equal(-1, info.Port);
            Assert.Equal("fb03d693-91fe-4dc5-8203-ff7a6390df66", info.Path);
            Assert.Null(info.Query);
            Assert.NotNull(info.Uris);
            Assert.Single(info.Uris);
            Assert.Equal("amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66", info.Uris[0]);
            Assert.Equal("amqp://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@192.168.0.81/fb03d693-91fe-4dc5-8203-ff7a6390df66", info.Uri);
            Assert.NotNull(info.ManagementUris);
            Assert.Single(info.ManagementUris);
            Assert.Equal("https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/", info.ManagementUris[0]);
            Assert.Equal("https://03c7a684-6ff1-4bd0-ad45-d10374ffb2af:l5oq2q0unl35s6urfsuib0jvpo@pivotal-rabbitmq.system.testcloud.com/api/", info.ManagementUri);
        }

        private static Service CreateRabbitMQService()
        {
            var environment = @"
                {
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

            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var opt = new CloudFoundryServicesOptions(config);
            var section = config.GetSection(CloudFoundryServicesOptions.ServicesConfigRoot);
            section.Bind(opt);
            Assert.Single(opt.Services);

            return opt.Services.First().Value.First();
        }
    }
}
