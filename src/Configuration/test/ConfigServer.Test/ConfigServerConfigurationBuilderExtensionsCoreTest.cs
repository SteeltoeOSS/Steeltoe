// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationBuilderExtensionsCoreTest
{
    private readonly Dictionary<string, string?> _quickTests = new()
    {
        ["spring:cloud:config:timeout"] = "10"
    };

    [Fact]
    public void AddConfigServer_AddsConfigServerProviderToProvidersList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_quickTests);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();

        configServerProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddConfigServer_WithLoggerFactorySucceeds()
    {
        CapturingLoggerProvider loggerProvider = new();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_quickTests);
        configurationBuilder.AddConfigServer(loggerFactory);
        _ = configurationBuilder.Build();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().Contain(
            "DBUG Steeltoe.Configuration.ConfigServer.ConfigServerConfigurationProvider: Fetching configuration from server at: http://localhost:8888/");
    }

    [Fact]
    public void AddConfigServer_JsonAppSettingsConfiguresClient()
    {
        const string appSettings = """
            {
              "spring": {
                "application": {
                  "name": "myName"
                },
                "cloud": {
                  "config": {
                    "uri": "https://user:password@foo.com:9999",
                    "enabled": false,
                    "failFast": false,
                    "label": "myLabel",
                    "username": "myUsername",
                    "password": "myPassword",
                    "timeout": 10000,
                    "token": "vault-token",
                    "tokenRenewRate": 50000,
                    "disableTokenRenewal": true,
                    "tokenTtl": 50000,
                    "retry": {
                      "enabled": "false",
                      "initialInterval": 55555,
                      "maxInterval": 55555,
                      "multiplier": 5.5,
                      "maxAttempts": 55555
                    }
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeFalse();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://user:password@foo.com:9999");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("myName");
        options.Label.Should().Be("myLabel");
        options.Username.Should().Be("myUsername");
        options.Password.Should().Be("myPassword");
        options.Retry.Enabled.Should().BeFalse();
        options.Retry.MaxAttempts.Should().Be(55555);
        options.Retry.InitialInterval.Should().Be(55555);
        options.Retry.MaxInterval.Should().Be(55555);
        options.Retry.Multiplier.Should().Be(5.5);
        options.Timeout.Should().Be(10_000);
        options.Token.Should().Be("vault-token");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.TokenRenewRate.Should().Be(50_000);
        options.DisableTokenRenewal.Should().BeTrue();
        options.TokenTtl.Should().Be(50_000);
    }

    [Fact]
    public void AddConfigServer_ValidateCertificates_DisablesCertValidation()
    {
        const string appSettings = """
            {
              "spring": {
                "cloud": {
                  "config": {
                    "validateCertificates": false,
                    "timeout": 0
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.ValidateCertificates.Should().BeFalse();
    }

    [Fact]
    public void AddConfigServer_Validate_Certificates_DisablesCertValidation()
    {
        const string appSettings = """
            {
              "spring": {
                "cloud": {
                  "config": {
                    "validate_certificates": false,
                    "timeout": 0
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.ValidateCertificates.Should().BeFalse();
    }

    [Fact]
    public void AddConfigServer_XmlAppSettingsConfiguresClient()
    {
        const string appSettings = """
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
            </settings>
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.xml", appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddXmlFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeFalse();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://foo.com:9999");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("myName");
        options.Label.Should().Be("myLabel");
        options.Username.Should().Be("myUsername");
        options.Password.Should().Be("myPassword");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_IniAppSettingsConfiguresClient()
    {
        const string appSettings = """
            [spring:cloud:config]
                uri=https://foo.com:9999
                enabled=false
                failFast=false
                label=myLabel
                name=myName
                username=myUsername
                password=myPassword
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.ini", appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddIniFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeFalse();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://foo.com:9999");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("myName");
        options.Label.Should().Be("myLabel");
        options.Username.Should().Be("myUsername");
        options.Password.Should().Be("myPassword");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_CommandLineAppSettingsConfiguresClient()
    {
        string[] appSettings =
        [
            "spring:cloud:config:enabled=false",
            "--spring:cloud:config:failFast=false",
            "/spring:cloud:config:uri=https://foo.com:9999",
            "--spring:cloud:config:name",
            "myName",
            "/spring:cloud:config:label",
            "myLabel",
            "--spring:cloud:config:username",
            "myUsername",
            "--spring:cloud:config:password",
            "myPassword"
        ];

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCommandLine(appSettings);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeFalse();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://foo.com:9999");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("myName");
        options.Label.Should().Be("myLabel");
        options.Username.Should().Be("myUsername");
        options.Password.Should().Be("myPassword");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_SubstitutesPlaceholders()
    {
        const string appSettings = """
            {
              "foo": {
                "bar": {
                  "name": "testName"
                }
              },
              "spring": {
                "application": {
                  "name": "myName"
                },
                "cloud": {
                  "config": {
                    "uri": "https://user:password@foo.com:9999",
                    "enabled": false,
                    "failFast": false,
                    "name": "${foo:bar:name?foobar}",
                    "label": "myLabel",
                    "username": "myUsername",
                    "password": "myPassword"
                  }
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);

        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddPlaceholderResolver();
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeFalse();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://user:password@foo.com:9999");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("testName");
        options.Label.Should().Be("myLabel");
        options.Username.Should().Be("myUsername");
        options.Password.Should().Be("myPassword");
    }

    [Fact]
    public void AddConfigServer_WithCloudfoundryEnvironment_ConfiguresClientCorrectly()
    {
        const string vcapApplication = """
            {
              "vcap": {
                "application": {
                  "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                  "application_name": "my-app",
                  "application_uris": [
                    "my-app.10.244.0.34.xip.io"
                  ],
                  "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                  "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                  },
                  "name": "my-app",
                  "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                  "space_name": "my-space",
                  "uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                  ],
                  "users": null,
                  "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
                }
              }
            }
            """;

        const string vcapServices = """
            {
              "vcap": {
                "services": {
                  "p-config-server": [
                    {
                      "credentials": {
                        "access_token_uri": "https://p-spring-cloud-services.uaa.wise.com/oauth/token",
                        "client_id": "p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef",
                        "client_secret": "e8KF1hXvAnGd",
                        "uri": "https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com"
                      },
                      "label": "p-config-server",
                      "name": "My Config Server",
                      "plan": "standard",
                      "tags": [
                        "configuration",
                        "spring-cloud"
                      ]
                    }
                  ]
                }
              }
            }
            """;

        const string appSettings = """
            {
              "spring": {
                "application": {
                  "name": "${vcap:application:name?foobar}"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string appSettingsPath = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string appSettingsFileName = Path.GetFileName(appSettingsPath);

        string vcapAppPath = sandbox.CreateFile("vcapapp.json", vcapApplication);
        string vcapAppFileName = Path.GetFileName(vcapAppPath);

        string vcapServicesPath = sandbox.CreateFile("vcapservices.json", vcapServices);
        string vcapServicesFileName = Path.GetFileName(vcapServicesPath);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(sandbox.FullPath);
        configurationBuilder.AddJsonFile(appSettingsFileName);
        configurationBuilder.AddJsonFile(vcapAppFileName);
        configurationBuilder.AddJsonFile(vcapServicesFileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com");
        options.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.wise.com/oauth/token");
        options.ClientId.Should().Be("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef");
        options.ClientSecret.Should().Be("e8KF1hXvAnGd");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("my-app");
        options.Label.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
    }

    [Fact]
    public void AddConfigServer_WithCloudfoundryEnvironmentSCS3_ConfiguresClientCorrectly()
    {
        const string vcapApplication = """
            {
              "vcap": {
                "application": {
                  "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                  "application_name": "my-app",
                  "application_uris": [
                    "my-app.10.244.0.34.xip.io"
                  ],
                  "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                  "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                  },
                  "name": "my-app",
                  "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                  "space_name": "my-space",
                  "uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                  ],
                  "users": null,
                  "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
                }
              }
            }
            """;

        const string vcapServices = """
            {
              "vcap": {
                "services": {
                  "p.config-server": [
                    {
                      "binding_name": "",
                      "credentials": {
                        "client_secret": "e8KF1hXvAnGd",
                        "uri": "https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com",
                        "client_id": "config-client-ea5e13c2-def2-4a3b-b80c-38e690ec284f",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.wise.com/oauth/token"
                      },
                      "instance_name": "myConfigServer",
                      "label": "p.config-server",
                      "name": "myConfigServer",
                      "plan": "standard",
                      "provider": null,
                      "syslog_drain_url": null,
                      "tags": [
                        "configuration",
                        "spring-cloud"
                      ],
                      "volume_mounts": []
                    }
                  ]
                }
              }
            }
            """;

        const string appSettings = """
            {
              "spring": {
                "application": {
                  "name": "${vcap:application:name?foobar}"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string appSettingsPath = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string appSettingsFileName = Path.GetFileName(appSettingsPath);

        string vcapAppPath = sandbox.CreateFile("vcapapp.json", vcapApplication);
        string vcapAppFileName = Path.GetFileName(vcapAppPath);

        string vcapServicesPath = sandbox.CreateFile("vcapservices.json", vcapServices);
        string vcapServicesFileName = Path.GetFileName(vcapServicesPath);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(sandbox.FullPath);
        configurationBuilder.AddJsonFile(appSettingsFileName);
        configurationBuilder.AddJsonFile(vcapAppFileName);
        configurationBuilder.AddJsonFile(vcapServicesFileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().SingleOrDefault();
        configServerProvider.Should().NotBeNull();

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeFalse();
        options.Uri.Should().Be("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com");
        options.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.wise.com/oauth/token");
        options.ClientId.Should().Be("config-client-ea5e13c2-def2-4a3b-b80c-38e690ec284f");
        options.ClientSecret.Should().Be("e8KF1hXvAnGd");
        options.Environment.Should().Be("Production");
        options.Name.Should().Be("my-app");
        options.Label.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
    }
}
