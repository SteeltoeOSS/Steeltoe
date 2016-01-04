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

namespace Spring.Extensions.Configuration.Server.IntegrationTest
{
    public class ConfigServerConfigurationExtensionsIntegrationTest
    {
        public ConfigServerConfigurationExtensionsIntegrationTest()
        {
        }

        [Fact]
        public void SpringCloudConfigServer_DefaultData()
        {
            // Arrange
            var appsettings = @"
{
    'spring': {
      'cloud': {
        'config': {
            'uri': 'http://localhost:8888',
            'name': 'foo',
            'environment': 'development'
        }
      }
    }
}";

            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer();
            IConfigurationRoot root = configurationBuilder.Build();

            Assert.Equal("spam", root["bar"]);
            Assert.Equal("bar", root["foo"]);
            Assert.Equal("Spring Cloud Samples", root["info:description"]);
            Assert.Equal("https://github.com/spring-cloud-samples", root["info:url"]);
            Assert.Equal("http://localhost:8761/eureka/", root["eureka:client:serviceUrl:defaultZone"]);

        }

    }
}

