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

using Spring.Extensions.Configuration.Server.Test;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
    //
    // NOTE: Some of the tests assume a running Spring Cloud Config Server is started
    //       with repository data for application: foo, profile: development
    //
    //       The easiest way to get that to happen is clone the spring-cloud-config
    //       repo and run the config-server.
    //          eg. git clone https://github.com/spring-cloud/spring-cloud-config.git
    //              cd spring-cloud-config\spring-cloud-config-server
    //              mvn spring-boot:run
    //

    public class ConfigServerConfigurationExtensionsIntegrationTest
    {
        public ConfigServerConfigurationExtensionsIntegrationTest()
        {
        }

        [Fact]
        public void SpringCloudConfigServer_ReturnsExpectedDefaultData()
        {

            // Arrange 
            var appsettings = @"
{
    'spring': {
      'application': {
        'name' : 'foo'
      },
      'cloud': {
        'config': {
            'uri': 'http://localhost:8888',
            'env': 'development'
        }
      }
    }
}";

            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            var hostingEnv = new HostingEnvironment();
            configurationBuilder.AddJsonFile(path);

            // Act and Assert (expects Spring Cloud Config server to be running)
            configurationBuilder.AddConfigServer(hostingEnv);
            IConfigurationRoot root = configurationBuilder.Build();

            Assert.Equal("spam", root["bar"]);
            Assert.Equal("bar", root["foo"]);
            Assert.Equal("Spring Cloud Samples", root["info:description"]);
            Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
            Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);

        }

        [Fact]
        public async void SpringCloudConfigServer_ReturnsExpectedDefaultData_AsInjectedOptions()
        {
            // Arrange
            var builder = new WebHostBuilder().UseStartup<TestServerStartup>();

            // Act and Assert (TestServer expects Spring Cloud Config server to be running)
            using (var server = new TestServer(builder)) { 
                var client = server.CreateClient();
                string result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

                Assert.Equal("spam" + 
                    "bar"+ 
                    "Spring Cloud Samples" +
                    "https://github.com/spring-cloud-samples", result);
            }
        }


        [Fact]
        public async void SpringCloudConfigServer_ConfiguredViaCloudfoundryEnv_ReturnsExpectedDefaultData_AsInjectedOptions()
        {
            // Arrange
            var VCAP_APPLICATION = @" 
{

    'application_id': 'fa05c1a9-0fc1-4fbd-bae1-139850dec7a3',
    'application_name': 'foo',
    'application_uris': [
    'foo.10.244.0.34.xip.io'
    ],
    'application_version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca',
    'limits': {
    'disk': 1024,
    'fds': 16384,
    'mem': 256
    },
    'name': 'foo',
    'space_id': '06450c72-4669-4dc6-8096-45f9777db68a',
    'space_name': 'my-space',
    'uris': [
    'foo.10.244.0.34.xip.io',
    'foo.10.244.0.34.xip.io'
    ],
    'users': null,
    'version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca'
}";


            var VCAP_SERVICES = @"
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

            System.Environment.SetEnvironmentVariable("VCAP_APPLICATION", VCAP_APPLICATION);
            System.Environment.SetEnvironmentVariable("VCAP_SERVICES", VCAP_SERVICES);

            // TestServerCloudfoundryStartup uses AddCloudfoundry() which parses VCAP_APPLICATION/VCAP_SERVICES
            var builder = new WebHostBuilder().UseStartup<TestServerCloudfoundryStartup>()
                                                .UseEnvironment("development");

            // Act and Assert (TestServer expects Spring Cloud Config server to be running)
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                string result = await client.GetStringAsync("http://localhost/Home/VerifyAsInjectedOptions");

                Assert.Equal("spam" +
                    "bar" +
                    "Spring Cloud Samples" +
                    "https://github.com/spring-cloud-samples", result);
            }
        }
    }
}

