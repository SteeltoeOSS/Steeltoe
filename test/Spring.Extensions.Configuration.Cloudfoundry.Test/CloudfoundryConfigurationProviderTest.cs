using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Spring.Extensions.Configuration.Cloudfoundry;

namespace Spring.Extensions.Configuration.Cloudfoundry.Test
{
    public class CloudfoundryConfigurationProviderTest
    {
        public CloudfoundryConfigurationProviderTest()
        {
        }

        [Fact]
        public void LoadVCAP_APPLICATION_ChangesDataDictionary()
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
            var provider = new CloudfoundryConfigurationProvider();

            // Act and Assert
            provider.Load();
            IDictionary<string, string> dict = provider.Properties;
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", dict["vcap:application:application_id"]);
            Assert.Equal("1024", dict["vcap:application:limits:disk"]);
            Assert.Equal("my-app.10.244.0.34.xip.io", dict["vcap:application:uris:0"]);
            Assert.Equal("my-app2.10.244.0.34.xip.io", dict["vcap:application:uris:1"]);
        }

        [Fact]
        public void LoadVCAP_SERVICES_ChangesDataDictionary()
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
            var provider = new CloudfoundryConfigurationProvider();

            // Act and Assert
            provider.Load();
            IDictionary<string, string> dict = provider.Properties;
            Assert.Equal("elephantsql-c6c60", dict["vcap:services:elephantsql:0:name"]);
            //Assert.Equal("1024", dict["vcap:application:limits:disk"]);
            //Assert.Equal("my-app.10.244.0.34.xip.io", dict["vcap:application:uris:0"]);
            //Assert.Equal("my-app2.10.244.0.34.xip.io", dict["vcap:application:uris:1"]);
        }
    }
}
