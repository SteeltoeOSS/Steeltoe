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


using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using SteelToe.Extensions.Configuration.ConfigServer.Test;

namespace SteelToe.Extensions.Configuration.ConfigServer.ITest
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

            var path = TestHelpers.CreateTempFile(appsettings);
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
    }
}

