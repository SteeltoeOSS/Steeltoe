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

using System;
using System.Collections.Generic;
using Xunit;

namespace Spring.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryConfigurationProviderTest
    {
        public CloudFoundryConfigurationProviderTest()
        {
        }

        [Fact]
        public void Load_VCAP_APPLICATION_ChangesDataDictionary()
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

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment);
            var provider = new CloudFoundryConfigurationProvider();

            // Act and Assert
            provider.Load();
            IDictionary<string, string> dict = provider.Properties;
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", dict["vcap:application:application_id"]);
            Assert.Equal("1024", dict["vcap:application:limits:disk"]);
            Assert.Equal("my-app.10.244.0.34.xip.io", dict["vcap:application:uris:0"]);
            Assert.Equal("my-app2.10.244.0.34.xip.io", dict["vcap:application:uris:1"]);

            Assert.Equal("my-app", dict["spring:application:name"]);
        }

        [Fact]
        public void Load_VCAP_SERVICES_ChangesDataDictionary()
        {
            var environment = @"
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
}";


            Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);
            var provider = new CloudFoundryConfigurationProvider();

            // Act and Assert
            provider.Load();
            IDictionary<string, string> dict = provider.Properties;
            Assert.Equal("elephantsql-c6c60", dict["vcap:services:elephantsql:0:name"]);
            Assert.Equal("mysendgrid", dict["vcap:services:sendgrid:0:name"]);

        }
    }

}
