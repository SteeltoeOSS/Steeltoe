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

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Rabbit.Test
{
    public class RabbitConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RabbitProviderConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = @"
{
   'rabbit': {
        'client': {
            'server': 'localhost',
            'port': 1234,
            'password': 'password',
            'username': 'username'
        }
   }
}";

            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var sconfig = new RabbitProviderConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Server);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("username", sconfig.Username);
            Assert.Equal(null, sconfig.Uri);
        }
    }
}
