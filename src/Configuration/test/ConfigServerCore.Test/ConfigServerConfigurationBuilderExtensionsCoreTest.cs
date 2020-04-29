﻿// Copyright 2017 the original author or authors.
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerConfigurationBuilderExtensionsCoreTest
    {
        private readonly Dictionary<string, string> quickTests = new Dictionary<string, string> { { "spring:cloud:config:timeout", "10" } };

        [Fact]
        public void AddConfigServer_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null;
            var environment = HostingHelpers.GetHostingEnvironment();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddConfigServer(environment));
            Assert.Contains(nameof(configurationBuilder), ex.Message);
        }

        [Fact]
        public void AddConfigServer_ThrowsIfHostingEnvironmentNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            IHostEnvironment env = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddConfigServer(env));
            Assert.Contains("environment", ex.Message);
        }

        [Fact]
        public void AddConfigServer_AddsConfigServerProviderToProvidersList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(quickTests);
            var environment = HostingHelpers.GetHostingEnvironment();

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            var config = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
        }

        [Fact]
        public void AddConfigServer_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(quickTests);
            var loggerFactory = new LoggerFactory();
            var environment = HostingHelpers.GetHostingEnvironment("Production");

            // Act and Assert
            configurationBuilder.AddConfigServer(environment, loggerFactory);
            var config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            Assert.NotNull(configServerProvider.Logger);
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
                            ""tokenRenewRate"": 50000,
                            ""disableTokenRenewal"": true,    
                            ""tokenTtl"": 50000,
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
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

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
            Assert.Null(settings.AccessTokenUri);
            Assert.Null(settings.ClientId);
            Assert.Null(settings.ClientSecret);
            Assert.Equal(50000, settings.TokenRenewRate);
            Assert.True(settings.DisableTokenRenewal);
            Assert.Equal(50000, settings.TokenTtl);
        }

        [Fact]
        public void AddConfigServer_ValidateCertificates_DisablesCertValidation()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                      ""cloud"": {
                        ""config"": {
                            ""validateCertificates"": false,
                            ""timeout"": 0
                        }
                      }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            var config = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.ValidateCertificates);
        }


        [Fact]
        public void AddConfigServer_Validate_Certificates_DisablesCertValidation()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                      ""cloud"": {
                        ""config"": {
                            ""validate_certificates"": false,
                            ""timeout"": 0
                        }
                      }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            var config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);

            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.ValidateCertificates);
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
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddXmlFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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
    uri=https://foo.com:9999
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

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddIniFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            // Act and Assert
            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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
                    "--spring:cloud:config:failFast=false",
                    "/spring:cloud:config:uri=https://foo.com:9999",
                    "--spring:cloud:config:name", "myName",
                    "/spring:cloud:config:label", "myLabel",
                    "--spring:cloud:config:username", "myUsername",
                    "--spring:cloud:config:password", "myPassword"
                };

            var configurationBuilder = new ConfigurationBuilder();
            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddCommandLine(appsettings);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();

            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

            Assert.False(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://foo.com:9999", settings.Uri);
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

            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            var environment = HostingHelpers.GetHostingEnvironment("Production");
            configurationBuilder.AddJsonFile(fileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);
            ConfigServerClientSettings settings = configServerProvider.Settings;

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
        public void AddConfigServer_WithCloudfoundryEnvironment_ConfiguresClientCorrectly()
        {
            // Arrange
            var vcap_application = @" 
                {
                    ""vcap"": {
                        ""application"": {
                          ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                          ""application_name"": ""my-app"",
                          ""application_uris"": [
                            ""my-app.10.244.0.34.xip.io""
                          ],
                          ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                          ""limits"": {
                            ""disk"": 1024,
                            ""fds"": 16384,
                            ""mem"": 256
                          },
                          ""name"": ""my-app"",
                          ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                          ""space_name"": ""my-space"",
                          ""uris"": [
                            ""my-app.10.244.0.34.xip.io"",
                            ""my-app2.10.244.0.34.xip.io""
                          ],
                          ""users"": null,
                          ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                        }
                    }
                }";

            var vcap_services = @"
                {
                    ""vcap"": {
                        ""services"": {
                            ""p-config-server"": [{
                                ""credentials"": {
                                    ""access_token_uri"": ""https://p-spring-cloud-services.uaa.wise.com/oauth/token"",
                                    ""client_id"": ""p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef"",
                                    ""client_secret"": ""e8KF1hXvAnGd"",
                                    ""uri"": ""https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com""
                                },
                                ""label"": ""p-config-server"",
                                ""name"": ""My Config Server"",
                                ""plan"": ""standard"",
                                ""tags"": [
                                    ""configuration"",
                                    ""spring-cloud""
                                ]
                            }]
                        }
                    }
                }";

            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""${vcap:application:name?foobar}""   
                        }
                    }
                }";
            var tempPath = Path.GetTempPath();
            var appsettingsPath = TestHelpers.CreateTempFile(appsettings);
            string appsettingsfileName = Path.GetFileName(appsettingsPath);

            var vcapAppPath = TestHelpers.CreateTempFile(vcap_application);
            string vcapAppfileName = Path.GetFileName(vcapAppPath);

            var vcapServicesPath = TestHelpers.CreateTempFile(vcap_services);
            string vcapServicesfileName = Path.GetFileName(vcapServicesPath);

            var environment = HostingHelpers.GetHostingEnvironment("Production");

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(tempPath);
            configurationBuilder.AddJsonFile(appsettingsfileName);
            configurationBuilder.AddJsonFile(vcapAppfileName);
            configurationBuilder.AddJsonFile(vcapServicesfileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

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

        [Fact]
        public void AddConfigServer_WithCloudfoundryEnvironmentSCS3_ConfiguresClientCorrectly()
        {
            // Arrange
            var vcap_application = @" 
                {
                    ""vcap"": {
                        ""application"": {
                            ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                            ""application_name"": ""my-app"",
                            ""application_uris"": [
                                ""my-app.10.244.0.34.xip.io""
                            ],
                            ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                            ""limits"": {
                                ""disk"": 1024,
                                ""fds"": 16384,
                                ""mem"": 256
                            },
                            ""name"": ""my-app"",
                            ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                            ""space_name"": ""my-space"",
                            ""uris"": [
                                ""my-app.10.244.0.34.xip.io"",
                                ""my-app2.10.244.0.34.xip.io""
                            ],
                            ""users"": null,
                            ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                        }
                    }
                }";

            var vcap_services = @"
                {
                    ""vcap"": {
                        ""services"": {
                            ""p.config-server"": [{
                                ""binding_name"":"""",
                                ""credentials"": {
                                     ""client_secret"":""e8KF1hXvAnGd"",
                                     ""uri"":""https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com"",
                                     ""client_id"":""config-client-ea5e13c2-def2-4a3b-b80c-38e690ec284f"",
                                     ""access_token_uri"":""https://p-spring-cloud-services.uaa.wise.com/oauth/token""
                                },
                                ""instance_name"": ""myConfigServer"",
                                ""label"": ""p.config-server"",
                                ""name"": ""myConfigServer"",
                                ""plan"": ""standard"",
                                ""provider"": null,
                                ""syslog_drain_url"": null,
                                ""tags"": [
                                    ""configuration"",
                                    ""spring-cloud""
                                ],
                                ""volume_mounts"": []
                            }]
                         }
                    }
                }";

            var appsettings = @"
                {
                    ""spring"": {
                        ""application"": {
                            ""name"": ""${vcap:application:name?foobar}""   
                        }
                    }
                }";
            var tempPath = Path.GetTempPath();
            var appsettingsPath = TestHelpers.CreateTempFile(appsettings);
            string appsettingsfileName = Path.GetFileName(appsettingsPath);

            var vcapAppPath = TestHelpers.CreateTempFile(vcap_application);
            string vcapAppfileName = Path.GetFileName(vcapAppPath);

            var vcapServicesPath = TestHelpers.CreateTempFile(vcap_services);
            string vcapServicesfileName = Path.GetFileName(vcapServicesPath);

            var environment = HostingHelpers.GetHostingEnvironment("Production");

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(tempPath);
            configurationBuilder.AddJsonFile(appsettingsfileName);
            configurationBuilder.AddJsonFile(vcapAppfileName);
            configurationBuilder.AddJsonFile(vcapServicesfileName);

            // Act and Assert
            configurationBuilder.AddConfigServer(environment);
            IConfigurationRoot config = configurationBuilder.Build();
            ConfigServerConfigurationProvider configServerProvider = config.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

            Assert.NotNull(configServerProvider);

            // Check settings
            ConfigServerClientSettings settings = configServerProvider.Settings;
            Assert.True(settings.Enabled);
            Assert.False(settings.FailFast);
            Assert.Equal("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com", settings.Uri);
            Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", settings.AccessTokenUri);
            Assert.Equal("config-client-ea5e13c2-def2-4a3b-b80c-38e690ec284f", settings.ClientId);
            Assert.Equal("e8KF1hXvAnGd", settings.ClientSecret);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal("my-app", settings.Name);
            Assert.Null(settings.Label);
            Assert.Null(settings.Username);
            Assert.Null(settings.Password);
        }
    }
}
