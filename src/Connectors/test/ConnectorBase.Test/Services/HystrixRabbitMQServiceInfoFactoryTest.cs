// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class HystrixRabbitMQServiceInfoFactoryTest
    {
        public HystrixRabbitMQServiceInfoFactoryTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var s = CreateHystrixService();
            Assert.NotNull(s);

            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsNoLabelNoTagsServiceBinding()
        {
            var s = new Service()
            {
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

            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsNoLabelNoTagsSecureUriServiceBinding()
        {
            var s = new Service()
            {
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
                            { "uri", new Credential("amqps://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120") },
                            { "ssl", new Credential("false") }
                        }
                    }
                }
            };
            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsWithLabelNoTagsServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-circuit-breaker-dashboard",
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
                            { "uri", new Credential("amqps://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120") },
                            { "ssl", new Credential("false") }
                        }
                    }
                }
            };

            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsMySQLServiceBinding()
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
            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsRabbitMQServiceBinding()
        {
            var s = new Service()
            {
                Label = "rabbitmq",
                Tags = new string[] { "rabbitmq", "rabbit" },
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
            var factory = new HystrixRabbitMQServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var s = CreateHystrixService();
            var factory = new HystrixRabbitMQServiceInfoFactory();
            var info = factory.Create(s) as HystrixRabbitMQServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("myHystrixService", info.Id);
            Assert.Equal("1clgf5ipeop36437dmr2em4duk", info.Password);
            Assert.Equal("a0f39f25-28a2-438e-a0e7-6c09d6d34dbd", info.UserName);
            Assert.Equal("192.168.1.55", info.Host);
            Assert.Equal(-1, info.Port);
            Assert.Equal("06f0b204-9f95-4829-a662-844d3c3d6120", info.Path);
            Assert.Null(info.Query);
            Assert.NotNull(info.Uris);
            Assert.Single(info.Uris);
            Assert.Equal("amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120", info.Uris[0]);
            Assert.Equal("amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120", info.Uri);
            Assert.False(info.IsSslEnabled);
         }

        private static Service CreateHystrixService()
        {
            var environment = @"
                {
                    ""p-circuit-breaker-dashboard"": [{
                        ""credentials"": {
                            ""stream"": ""https://turbine-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com"",
                            ""amqp"": {
                                ""http_api_uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/""],
                                ""ssl"": false,
                                ""dashboard_url"": ""https://pivotal-rabbitmq.system.testcloud.com/#/login/a0f39f25-28a2-438e-a0e7-6c09d6d34dbd/1clgf5ipeop36437dmr2em4duk"",
                                ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                ""protocols"": {
                                    ""amqp"": {
                                        ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""port"": 5672,
                                        ""host"": ""192.168.1.55"",
                                        ""hosts"": [""192.168.1.55""],
                                        ""ssl"": false,
                                        ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                        ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:5672/06f0b204-9f95-4829-a662-844d3c3d6120""]
                                    },
                                    ""management"": {
                                        ""path"": ""/api/"",
                                        ""ssl"": false,
                                        ""hosts"": [""192.168.1.55""],
                                        ""password"": ""1clgf5ipeop36437dmr2em4duk"",
                                        ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                        ""port"": 15672,
                                        ""host"": ""192.168.1.55"",
                                        ""uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/"",
                                        ""uris"": [""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55:15672/api/""]
                                    }
                                },
                                ""username"": ""a0f39f25-28a2-438e-a0e7-6c09d6d34dbd"",
                                ""hostname"": ""192.168.1.55"",
                                ""hostnames"": [""192.168.1.55""],
                                ""vhost"": ""06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""http_api_uri"": ""https://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@pivotal-rabbitmq.system.testcloud.com/api/"",
                                ""uri"": ""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120"",
                                ""uris"": [""amqp://a0f39f25-28a2-438e-a0e7-6c09d6d34dbd:1clgf5ipeop36437dmr2em4duk@192.168.1.55/06f0b204-9f95-4829-a662-844d3c3d6120""]
                            },
                            ""dashboard"": ""https://hystrix-5ac7e504-3ca5-4f02-9302-d5554c059043.apps.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-circuit-breaker-dashboard"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myHystrixService"",
                        ""tags"": [
                            ""circuit-breaker"",
                            ""hystrix-amqp"",
                            ""spring-cloud""
                        ]
                    }]
                }";

            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var opt = new CloudFoundryServicesOptions();
            var section = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            section.Bind(opt);
            Assert.Single(opt.Services);

            return opt.Services.First().Value[0];
        }
    }
}
