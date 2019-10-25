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
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.IO;
using System.Linq;
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
        public void AddConfigServer_AddsConfigServerSourceToList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var settings = new ConfigServerClientSettings();

            // Act and Assert
            configurationBuilder.AddConfigServer(settings);

            var configServerSource = configurationBuilder.Sources.OfType<ConfigServerConfigurationSource>().SingleOrDefault();
            Assert.NotNull(configServerSource);
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

            var configServerSource = configurationBuilder.Sources.OfType<ConfigServerConfigurationSource>().SingleOrDefault();

            Assert.NotNull(configServerSource);
            Assert.NotNull(configServerSource.LogFactory);
        }

        [Fact]
        public void AddConfigServer_JsonAppSettingsConfiguresClient()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""https://user:password@foo.com:9999"",
                            ""enabled"": false,
                            ""failFast"": false,
                            ""label"": ""myLabel"",
                            ""username"": ""myUsername"",
                            ""password"": ""myPassword"",
                            ""timeout"": 10000,
                            ""token"" : ""vaulttoken"",
                            ""retry"": {
                                ""enabled"":""false"",
                                ""initialInterval"":55555,
                                ""maxInterval"": 55555,
                                ""multiplier"": 5.5,
                                ""maxAttempts"": 55555
                            }
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            var config = configurationBuilder.Build();

            var configServerProvider =
                config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
            Assert.NotNull(configServerProvider);

            var settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://user:password@foo.com:9999", settings.Uri);
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
            <uri>https://foo.com:9999</uri>
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
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddXmlFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            var config = configurationBuilder.Build();

            var configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();

            Assert.NotNull(configServerProvider);
            var settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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
    uri=https://foo.com:9999
    enabled=false
    failFast=false
    label=myLabel
    name=myName
    username=myUsername
    password=myPassword
";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddIniFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            var config = configurationBuilder.Build();

            var configServerProvider =
                config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            var settings = configServerProvider.Settings;

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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
                    "/spring:cloud:config:uri=https://foo.com:9999",
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
            var config = configurationBuilder.Build();
            var configServerProvider =
                config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            var settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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
                    ""foo"": {
                        ""bar"": {
                            ""name"": ""testName""
                        },
                    },
                    ""spring"": {
                        ""application"": {
                            ""name"": ""myName""
                        },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""https://user:password@foo.com:9999"",
                            ""enabled"": false,
                            ""failFast"": false,
                            ""name"": ""${foo:bar:name?foobar}"",
                            ""label"": ""myLabel"",
                            ""username"": ""myUsername"",
                            ""password"": ""myPassword""
                        }
                      }
                    }
                }";

            var path = TestHelpers.CreateTempFile(appsettings);

            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var csettings = new ConfigServerClientSettings();
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(csettings);
            var config = configurationBuilder.Build();

            var configServerProvider =
                config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            var settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://user:password@foo.com:9999", settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("testName", settings.Name);
            Assert.Equal("myLabel", settings.Label);
            Assert.Equal("myUsername", settings.Username);
            Assert.Equal("myPassword", settings.Password);
        }

        [Fact]
        public void AddConfigServer_VCAP_SERVICES_Override_Defaults()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            const string vcap_application = @" 
                {
                    ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                    ""application_name"": ""foo"",
                    ""application_uris"": [
                        ""foo.10.244.0.34.xip.io""
                    ],
                    ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                    ""limits"": {
                        ""disk"": 1024,
                        ""fds"": 16384,
                        ""mem"": 256
                    },
                    ""name"": ""foo"",
                    ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                    ""space_name"": ""my-space"",
                    ""uris"": [
                        ""foo.10.244.0.34.xip.io""
                    ],
                    ""users"": null,
                    ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                }";

            const string vcap_services = @"
                {
                    ""p-config-server"": [
                    {
                        ""name"": ""config-server"",
                        ""instance_name"": ""config-server"",
                        ""binding_name"": null,
                        ""credentials"": {
                            ""uri"": ""https://uri-from-vcap-services"",
                            ""client_secret"": ""some-secret"",
                            ""client_id"": ""some-client-id"",
                            ""access_token_uri"": ""https://uaa-uri-from-vcap-services/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""volume_mounts"": [],
                        ""label"": ""p-config-server"",
                        ""plan"": ""standard"",
                        ""provider"": null,
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }]
                }";
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);
            var settings = new ConfigServerClientSettings() { Uri = "https://uri-from-settings" };

            // Act
            configurationBuilder
                .AddEnvironmentVariables()
                .AddConfigServer(settings);
            var config = configurationBuilder.Build();
            var configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();

            // Assert
            Assert.NotNull(configServerProvider);
            Assert.IsType<ConfigServerConfigurationProvider>(configServerProvider);

            Assert.NotEqual("https://uri-from-settings", configServerProvider.Settings.Uri);
            Assert.Equal("https://uri-from-vcap-services", configServerProvider.Settings.Uri);

            // reset to avoid breaking other tests
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", string.Empty);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", string.Empty);
        }

        [Fact]
        public void AddConfigServer_PaysAttentionToSettings()
        {
            // Arrange
            var configServerClientSettings = new ConfigServerClientSettings()
            {
                Name = "testConfigName",
                Label = "testConfigLabel",
                Environment = "testEnv",
                Username = "testUser",
                Password = "testPassword"
            };
            var builder = new ConfigurationBuilder().AddConfigServer(configServerClientSettings);

            // Act
            var config = builder.Build();
            var provider = config.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();

            // Assert
            Assert.NotNull(provider);
            Assert.Equal("testConfigLabel", provider.Settings.Label);
            Assert.Equal("testConfigName", provider.Settings.Name);
            Assert.Equal("testEnv", provider.Settings.Environment);
            Assert.Equal("testUser", provider.Settings.Username);
            Assert.Equal("testPassword", provider.Settings.Password);
        }

        [Fact]
        public void AddConfigServer_AddsCloudFoundryConfigurationSource()
        {
            // arrange
            var configurationBuilder = new ConfigurationBuilder();

            // act
            configurationBuilder.AddConfigServer();

            // assert
            Assert.Single(configurationBuilder.Sources.OfType<CloudFoundryConfigurationSource>());
        }

        [Fact]
        public void AddConfigServer_Only_AddsOneCloudFoundryConfigurationSource()
        {
            // arrange
            var configurationBuilder = new ConfigurationBuilder();

            // act
            configurationBuilder.AddCloudFoundry(new CustomCloudFoundrySettingsReader());
            configurationBuilder.AddConfigServer();

            // assert
            Assert.Single(configurationBuilder.Sources.Where(c => c.GetType() == typeof(CloudFoundryConfigurationSource)));
        }
    }
}
