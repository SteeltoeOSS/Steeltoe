// Copyright 2017 the original author or authors.
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
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void AddConfigServer_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationBuilderExtensions.AddConfigServer(configurationBuilder, new ConfigServerClientSettings()));
            Assert.Contains(nameof(configurationBuilder), ex.Message);
        }

        [Fact]
        public void AddConfigServer_ThrowsIfSettingsNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            ConfigServerClientSettings defaultSettings = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationBuilderExtensions.AddConfigServer(configurationBuilder, defaultSettings));
            Assert.Contains(nameof(defaultSettings), ex.Message);
        }

        [Fact]
        public void AddConfigServer_AddsConfigServerProviderToProvidersList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var settings = new ConfigServerClientSettings();

            // Act and Assert
            configurationBuilder.AddConfigServer(settings);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
        }

        [Fact]
        public void AddConfigServer_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();
            var settings = new ConfigServerClientSettings();

            // Act and Assert
            configurationBuilder.AddConfigServer(settings, loggerFactory);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            Assert.NotNull(configServerProvider.Logger);
        }

        [Fact]
        public void AddConfigServer_JsonAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
{
    'spring': {
        'application': {
            'name': 'myName'
    },
      'cloud': {
        'config': {
            'uri': 'http://user:password@foo.com:9999',
            'enabled': false,
            'failFast': false,
            'label': 'myLabel',
            'username': 'myUsername',
            'password': 'myPassword',
            'timeout': 10000,
            'token' : 'vaulttoken',
            'retry': {
                'enabled':'false',
                'initialInterval':55555,
                'maxInterval': 55555,
                'multiplier': 5.5,
                'maxAttempts': 55555
            }
        }
      }
    }
}";

            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            configurationBuilder.Build();

            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("http://user:password@foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
            Assert.False(settings.RetryEnabled);
            Assert.Equal(55555, settings.RetryAttempts);
            Assert.Equal(55555, settings.RetryInitialInterval);
            Assert.Equal(55555, settings.RetryMaxInterval);
            Assert.Equal(5.5, settings.RetryMultiplier);
            Assert.Equal(10000, settings.Timeout);
            Assert.Equal("vaulttoken", settings.Token);
        }

        [Fact]
        public void AddConfigServer_XmlAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
<settings>
    <spring>
      <cloud>
        <config>
            <uri>http://foo.com:9999</uri>
            <enabled>false</enabled>
            <failFast>false</failFast>
            <label>myLabel</label>
            <name>myName</name>
            <username>myUsername</username>
            <password>myPassword</password>
        </config>
      </cloud>
    </spring>
</settings>";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddXmlFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            IConfigurationRoot root = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
        }

        [Fact]
        public void AddConfigServer_IniAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
[spring:cloud:config]
    uri=http://foo.com:9999
    enabled=false
    failFast=false
    label=myLabel
    name=myName
    username=myUsername
    password=myPassword
";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddIniFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            IConfigurationRoot root = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
        }

        [Fact]
        public void AddConfigServer_CommandLineAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = new string[]
                {
                    "spring:cloud:config:enabled=false",
                    "--spring:cloud:config:failFast=false",
                    "/spring:cloud:config:uri=http://foo.com:9999",
                    "--spring:cloud:config:name", "myName",
                    "/spring:cloud:config:label", "myLabel",
                    "--spring:cloud:config:username", "myUsername",
                    "--spring:cloud:config:password", "myPassword"
                };

            var configurationBuilder = new ConfigurationBuilder();
            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddCommandLine(appsettings);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            IConfigurationRoot root = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
        }

        [Fact]
        public void AddConfigServer_HandlesPlaceHolders()
        {
            // Arrange
            var appsettings = @"
{
    'foo': {
        'bar': {
            'name': 'testName'
        },
    },
    'spring': {
        'application': {
            'name': 'myName'
        },
      'cloud': {
        'config': {
            'uri': 'http://user:password@foo.com:9999',
            'enabled': false,
            'failFast': false,
            'name': '${foo:bar:name?foobar}',
            'label': 'myLabel',
            'username': 'myUsername',
            'password': 'myPassword'
        }
      }
    }
}";

            var path = TestHelpers.CreateTempFile(appsettings);

            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            IConfigurationRoot root = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationSource source in configurationBuilder.Sources)
            {
                configServerProvider = source as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                {
                    break;
                }
            }

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("http://user:password@foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("testName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
        }
    }
}
