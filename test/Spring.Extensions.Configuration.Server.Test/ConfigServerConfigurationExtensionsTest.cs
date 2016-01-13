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
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration.Xml;

namespace Spring.Extensions.Configuration.Server.Test
{
    public class ConfigServerConfigurationExtensionsTest
    {
        [Fact]
        public void AddConfigServer_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;
            var environment = new HostingEnvironment();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationExtensions.AddConfigServer(configurationBuilder, environment));
            Assert.Contains(nameof(configurationBuilder), ex.Message);

        }

        [Fact]
        public void AddConfigServer_ThrowsIfHostingEnvironmentNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            IHostingEnvironment environment = null;
            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => ConfigServerConfigurationExtensions.AddConfigServer(configurationBuilder, environment));
            Assert.Contains(nameof(environment), ex.Message);

        }

        [Fact]
        public void AddConfigServer_AddsConfigServerProviderToProvidersList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

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
        public void AddConfigServer_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();
            var environment = new HostingEnvironment();

            // Act and Assert
            configurationBuilder.AddConfigServer(environment,loggerFactory);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
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
            'failFast': true,
            'label': 'myLabel',
            'username': 'myUsername',
            'password': 'myPassword'
        }
      }
    }
}";

            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();
            configurationBuilder.AddJsonFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

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
            Assert.Equal("http://user:password@foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);
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
            <failFast>true</failFast>
            <label>myLabel</label>
            <name>myName</name>
            <username>myUsername</username>
            <password>myPassword</password>
        </config>
      </cloud>
    </spring>
</settings>";
            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();
            configurationBuilder.AddXmlFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

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
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);

        }
        [Fact]
        public void AddConfigServer_IniAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
[spring:cloud:config]
    uri=http://foo.com:9999
    enabled=false
    failFast=true
    label=myLabel
    name=myName
    username=myUsername
    password=myPassword
";
            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();
            configurationBuilder.AddIniFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
            }
            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);

        }

        [Fact]
        public void AddConfigServer_CommandLineAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = new string[]
                {
                    "spring:cloud:config:enabled=false",
                    "--spring:cloud:config:failFast=true",
                    "/spring:cloud:config:uri=http://foo.com:9999",
                    "--spring:cloud:config:name", "myName",
                    "/spring:cloud:config:label", "myLabel",
                    "--spring:cloud:config:username", "myUsername",
                    "--spring:cloud:config:password", "myPassword"
                };

            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();
            configurationBuilder.AddCommandLine(appsettings);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

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
            Assert.Equal("http://foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("myName", settings.Name );
            Assert.Equal("myLabel", settings.Label );
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password );
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);

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
            'failFast': true,
            'name': '${foo:bar:name?foobar}',
            'label': 'myLabel',
            'username': 'myUsername',
            'password': 'myPassword'
        }
      }
    }
}";

            var path = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            var environment = new HostingEnvironment();
            configurationBuilder.AddJsonFile(path);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);

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
            Assert.Equal("http://user:password@foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("testName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);
        }

        [Fact]
        public void AddConfigServer_WithCloudfoundryEnvironment_ConfiguresClientCorrectly()
        {

            // Arrange
            var VCAP_APPLICATION = @" 
{
'vcap': {
    'application': 
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
        }
    }
}";

            var VCAP_SERVICES = @"
{
'vcap': {
    'services': {
        'p-config-server': [
        {
        'credentials': {
         'access_token_uri': 'https://p-spring-cloud-services.uaa.wise.com/oauth/token',
         'client_id': 'p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef',
         'client_secret': 'e8KF1hXvAnGd',
         'uri': 'https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com'
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
    }
}
}";

            var appsettings = @"
{
    'spring': {
        'application': {
            'name': '${vcap:application:name?foobar}'   
        }
    }
}";

            var appsettingsPath = ConfigServerTestHelpers.CreateTempFile(appsettings);
            var vcapAppPath = ConfigServerTestHelpers.CreateTempFile(VCAP_APPLICATION);
            var vcapServicesPath = ConfigServerTestHelpers.CreateTempFile(VCAP_SERVICES);
            var environment = new HostingEnvironment();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(appsettingsPath);
            configurationBuilder.AddJsonFile(vcapAppPath);
            configurationBuilder.AddJsonFile(vcapServicesPath);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot root = configurationBuilder.Build();

            // Find our provider so we can check settings
            ConfigServerConfigurationProvider configServerProvider = null;
            foreach (IConfigurationProvider provider in configurationBuilder.Providers)
            {
                configServerProvider = provider as ConfigServerConfigurationProvider;
                if (configServerProvider != null)
                    break;
            }
            Assert.NotNull(configServerProvider);

            // Check settings
            ConfigServerClientSettings settings = configServerProvider.Settings;
            Assert.True(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com", settings.Uri);
            Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", settings.AccessTokenUri);
            Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", settings.ClientId);
            Assert.Equal("e8KF1hXvAnGd", settings.ClientSecret);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("my-app", settings.Name);
            Assert.Null(settings.Label);
            Assert.Null(settings.Username);
            Assert.Null(settings.Password);
        }
    }
}
