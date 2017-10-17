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
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Linq;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class HystrixRabbitServiceInfoFactoryTest
    {
        public HystrixRabbitServiceInfoFactoryTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            Service s = CreateHystrixService();
            Assert.NotNull(s);

            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsNoLabelNoTagsServiceBinding()
        {
            Service s = new Service()
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

            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsNoLabelNoTagsSecureUriServiceBinding()
        {
            Service s = new Service()
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
            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsWithLabelNoTagsServiceBinding()
        {
            Service s = new Service()
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

            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsMySQLServiceBinding()
        {
            Service s = new Service()
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
            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsRabbitServiceBinding()
        {
            Service s = new Service()
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
            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            Service s = CreateHystrixService();
            HystrixRabbitServiceInfoFactory factory = new HystrixRabbitServiceInfoFactory();
            var info = factory.Create(s) as HystrixRabbitServiceInfo;
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

        public static Service CreateHystrixService()
        {
            var environment = @"
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
  }";

            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();
            var opt = new CloudFoundryServicesOptions();
            config.Bind(opt);
            Assert.Single(opt.Services);

            return opt.Services.First().Value[0];
        }
    }
}
