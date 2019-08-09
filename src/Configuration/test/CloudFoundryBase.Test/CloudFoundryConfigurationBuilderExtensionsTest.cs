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
using System;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void AddCloudFoundry_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryConfigurationBuilderExtensions.AddCloudFoundry(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => CloudFoundryConfigurationBuilderExtensions.AddCloudFoundry(configurationBuilder, null));
            Assert.Contains(nameof(configurationBuilder), ex2.Message);
        }

        [Fact]
        public void AddCloudFoundry_AddsCloudFoundrySourceToSourcesList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddCloudFoundry();

            CloudFoundryConfigurationSource cloudSource = null;
            foreach (var source in configurationBuilder.Sources)
            {
                cloudSource = source as CloudFoundryConfigurationSource;
                if (cloudSource != null)
                {
                    break;
                }
            }

            Assert.NotNull(cloudSource);
        }

        [Fact]
        public void AddCloudFoundry_CanReadSettingsFromMemory()
        {
            var reader = new CloudFoundryMemorySettingsReader
            {
                ApplicationJson = @"
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
  }",
                ServicesJson = @"
{
  'elephantsql': [
    {
      'name': 'elephantsql-c6c60',
      'label': 'elephantsql',
      'tags': [
        'postgres',
        'postgresql',
        'relational'
      ],
      'plan': 'turtle',
      'credentials': {
        'uri': 'postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd'
      }
    }
  ],
  'sendgrid': [
    {
      'name': 'mysendgrid',
      'label': 'sendgrid',
      'tags': [
        'smtp'
      ],
      'plan': 'free',
      'credentials': {
        'hostname': 'smtp.sendgrid.net',
        'username': 'QvsXMbJ3rK',
        'password': 'HCHMOYluTv'
      }
    }
  ]
}",
                InstanceId = "7c19d892-21c2-496b-a42a-946bbaa0775e",
                InstanceIndex = "0",
                InstanceInternalIp = "127.0.0.1",
                InstanceIp = "10.41.1.1",
                InstancePort = "8888"
            };

            var configuration = new ConfigurationBuilder().AddCloudFoundry(reader).Build();

            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", configuration["vcap:application:application_id"]);
            Assert.Equal("1024", configuration["vcap:application:limits:disk"]);
            Assert.Equal("my-app.10.244.0.34.xip.io", configuration["vcap:application:uris:0"]);
            Assert.Equal("my-app2.10.244.0.34.xip.io", configuration["vcap:application:uris:1"]);
            Assert.Equal("elephantsql-c6c60", configuration["vcap:services:elephantsql:0:name"]);
            Assert.Equal("mysendgrid", configuration["vcap:services:sendgrid:0:name"]);

            Assert.Equal("7c19d892-21c2-496b-a42a-946bbaa0775e", configuration["vcap:application:instance_id"]);
            Assert.Equal("0", configuration["vcap:application:instance_index"]);
            Assert.Equal("127.0.0.1", configuration["vcap:application:internal_ip"]);
            Assert.Equal("10.41.1.1", configuration["vcap:application:instance_ip"]);
            Assert.Equal("8888", configuration["vcap:application:port"]);
        }
    }
}
