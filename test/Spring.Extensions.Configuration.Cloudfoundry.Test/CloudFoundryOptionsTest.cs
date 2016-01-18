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

using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.OptionsModel;

namespace Spring.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryOptionsTest
    {
        [Fact]
        public void ConfigureCloudFoundryOptions_With_VCAP_APPLICATION_Environment_NotPresent()
        {
            // Arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            var services = new ServiceCollection().AddOptions();
            // Act and Assert

            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();

            services.Configure<CloudFoundryApplicationOptions>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(service);
            var options = service.Value;

            Assert.Null(options.ApplicationId);
            Assert.Null(options.ApplicationName);
            Assert.Null(options.ApplicationUris);
            Assert.Null(options.ApplicationVersion);
            Assert.Null(options.InstanceId);
            Assert.Null(options.Name);
            Assert.Null(options.SpaceId);
            Assert.Null(options.SpaceName);
            Assert.Null(options.Start);
            Assert.Null(options.Uris);
            Assert.Equal(-1, options.DiskLimit);
            Assert.Equal(-1, options.FileDescriptorLimit);
            Assert.Equal(-1, options.InstanceIndex);
            Assert.Equal(-1, options.MemoryLimit);
            Assert.Equal(-1, options.Port);
        }

        [Fact]
        public void ConfigureCloudFoundryOptions_With_VCAP_SERVICES_Environment_NotPresent()
        {
            // Arrange
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
            var services = new ServiceCollection().AddOptions();
            // Act and Assert

            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();

            services.Configure<CloudFoundryServicesOptions>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            Assert.NotNull(options.Services);
            Assert.Equal(0, options.Services.Count);
        }

        [Fact]
        public void ConfigureCloudFoundryOptions_With_VCAP_APPLICATION_Environment_ConfiguresCloudFoundryApplicationOptions()
        {
            // Arrange
            var environment = @"
{
 
  'application_id': 'fa05c1a9-0fc1-4fbd-bae1-139850dec7a3',
  'application_name': 'my-app',
  'application_uris': [
    'my-app.10.244.0.34.xip.io'
  ],
  'application_version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca',
  'limits': {
    'disk': 1024,
    'fds': 16384,
    'mem': 256
  },
  'name': 'my-app',
  'space_id': '06450c72-4669-4dc6-8096-45f9777db68a',
  'space_name': 'my-space',
  'uris': [
    'my-app.10.244.0.34.xip.io',
    'my-app2.10.244.0.34.xip.io'
  ],
  'users': null,
  'version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca'
}";

            // Arrange
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment);
            var services = new ServiceCollection().AddOptions();
            // Act and Assert

            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();

            services.Configure<CloudFoundryApplicationOptions>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
            Assert.Equal("my-app", options.ApplicationName);

            Assert.NotNull(options.ApplicationUris);
            Assert.Equal(1, options.ApplicationUris.Length);
            Assert.Equal("my-app.10.244.0.34.xip.io", options.ApplicationUris[0]);

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.ApplicationVersion);
            Assert.Equal("my-app", options.Name);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.SpaceId);
            Assert.Equal("my-space", options.SpaceName);

            Assert.NotNull(options.Uris);
            Assert.Equal(2, options.Uris.Length);
            Assert.Equal("my-app.10.244.0.34.xip.io", options.Uris[0]);
            Assert.Equal("my-app2.10.244.0.34.xip.io", options.Uris[1]);

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }

        [Fact]
        public void ConfigureCloudFoundryOptions_With_VCAP_SERVICES_Environment_ConfiguresCloudFoundryServicesOptions()
        {
            // Arrange
            var environment = @"
{
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
}";
            // Arrange
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);
            var services = new ServiceCollection().AddOptions();
            // Act and Assert

            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();

            services.Configure<CloudFoundryServicesOptions>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options.Services);
            Assert.Equal(1, options.Services.Count);
            Assert.Equal("p-config-server", options.Services[0].Label);
            Assert.Equal("My Config Server", options.Services[0].Name);
            Assert.Equal("standard", options.Services[0].Plan);

            Assert.NotNull(options.Services[0].Tags);
            Assert.Equal(2, options.Services[0].Tags.Length);
            Assert.Equal("configuration", options.Services[0].Tags[0]);
            Assert.Equal("spring-cloud", options.Services[0].Tags[1]);

            Assert.NotNull(options.Services[0].Credentials);
            Assert.Equal(4, options.Services[0].Credentials.Count);
            Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", options.Services[0].Credentials["access_token_uri"].Value);
            Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", options.Services[0].Credentials["client_id"].Value);
            Assert.Equal("e8KF1hXvAnGd", options.Services[0].Credentials["client_secret"].Value);
            Assert.Equal("http://localhost:8888", options.Services[0].Credentials["uri"].Value);
        }
        [Fact]
        public void ConfigureCloudFoundryOptions_With_Complex_VCAP_SERVICES_Environment_ConfiguresCloudFoundryServicesOptions()
        {
            // Arrange
            var environment = @"
{
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
}";
            // Arrange
            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);
            var services = new ServiceCollection().AddOptions();
            // Act and Assert

            var builder = new ConfigurationBuilder().AddCloudFoundry();
            var config = builder.Build();

            services.Configure<CloudFoundryServicesOptions>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options.Services);
            Assert.Equal(1, options.Services.Count);
            Assert.Equal("p-rabbitmq", options.Services[0].Label);
            Assert.Equal("rabbitmq", options.Services[0].Name);
            Assert.Equal("standard", options.Services[0].Plan);

            Assert.NotNull(options.Services[0].Tags);
            Assert.Equal(7, options.Services[0].Tags.Length);
            Assert.Equal("rabbitmq", options.Services[0].Tags[0]);
            Assert.Equal("pivotal", options.Services[0].Tags[6]);

            Assert.NotNull(options.Services[0].Credentials);
            Assert.Equal(12, options.Services[0].Credentials.Count);
            Assert.Equal("https://pivotal-rabbitmq.system.testcloud.com/#/login/268371bd-07e5-46f3-aec7-d1633ae20bbb/3fnpvbqm0djq5jl9fp6fc697f4", options.Services[0].Credentials["dashboard_url"].Value);
            Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", options.Services[0].Credentials["username"].Value);
            Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", options.Services[0].Credentials["password"].Value);
            Assert.Equal("268371bd-07e5-46f3-aec7-d1633ae20bbb", options.Services[0].Credentials["protocols"]["amqp"]["username"].Value);
            Assert.Equal("3fnpvbqm0djq5jl9fp6fc697f4", options.Services[0].Credentials["protocols"]["amqp"]["password"].Value);
            Assert.Equal("amqp://268371bd-07e5-46f3-aec7-d1633ae20bbb:3fnpvbqm0djq5jl9fp6fc697f4@192.168.0.97:5672/2260a117-cf28-4725-86dd-37b3b8971052",
                options.Services[0].Credentials["protocols"]["amqp"]["uris"]["0"].Value);

            //}
        }
    }
}
