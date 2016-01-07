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
using Xunit;
using System;
using Microsoft.Extensions.Logging;

namespace Spring.Extensions.Configuration.Server.Test
{
    public class ConfigServerConfigurationExtensionsTest
    {
        [Fact]
        public void AddConfigService_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationExtensions.AddConfigServer(configurationBuilder));
            Assert.Contains(nameof(configurationBuilder), ex.Message);

        }

        [Fact]
        public void AddConfigService_AddsConfigServerProviderToProvidersList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddConfigServer();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
            }
            Assert.NotNull(configServerProvider);

        }

        [Fact]
        public void AddConfigService_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();

            // Act and Assert
            configurationBuilder.AddConfigServer(loggerFactory);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
            }

            Assert.NotNull(configServerProvider);
            Assert.NotNull(configServerProvider.Logger);
            Assert.NotNull(configServerProvider.Settings.Logger);

        }

        [Fact]
        public void AddConfigService_JsonAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
{
    'spring': {
      'cloud': {
        'config': {
            'uri': 'http://foo.com:9999',
            'enabled': false,
            'failFast': true,
            'label': 'myLabel',
            'name': 'myName',
            'username': 'myUsername',
            'password': 'myPassword'
        }
      }
    }
}";

            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
            }
            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(settings.Uri, "http://foo.com:9999");
            Assert.Equal(settings.Environment, "Development");
            Assert.Equal(settings.Name, "myName");
            Assert.Equal(settings.Label, "myLabel");
            Assert.Equal(settings.Username, "myUsername");
            Assert.Equal(settings.Password, "myPassword");
        }

    }
}
