// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationBuilderExtensionsCoreTest
{
    private readonly Dictionary<string, string?> _quickTests = new()
    {
        { "spring:cloud:config:timeout", "10" }
    };

    [Fact]
    public void AddConfigServer_AddsConfigServerProviderToProvidersList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_quickTests);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();

        Assert.NotNull(configServerProvider);
    }

    [Fact]
    public void AddConfigServer_WithLoggerFactorySucceeds()
    {
        CapturingLoggerProvider loggerProvider = new();
        var loggerFactory = new LoggerFactory([loggerProvider]);

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
        const string appsettings = """
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
                    "token": "vaulttoken",
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://user:password@foo.com:9999", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("myName", options.Name);
        Assert.Equal("myLabel", options.Label);
        Assert.Equal("myUsername", options.Username);
        Assert.Equal("myPassword", options.Password);
        Assert.False(options.Retry.Enabled);
        Assert.Equal(55555, options.Retry.MaxAttempts);
        Assert.Equal(55555, options.Retry.InitialInterval);
        Assert.Equal(55555, options.Retry.MaxInterval);
        Assert.Equal(5.5, options.Retry.Multiplier);
        Assert.Equal(10000, options.Timeout);
        Assert.Equal("vaulttoken", options.Token);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
        Assert.Equal(50000, options.TokenRenewRate);
        Assert.True(options.DisableTokenRenewal);
        Assert.Equal(50000, options.TokenTtl);
    }

    [Fact]
    public void AddConfigServer_ValidateCertificates_DisablesCertValidation()
    {
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.ValidateCertificates);
    }

    [Fact]
    public void AddConfigServer_Validate_Certificates_DisablesCertValidation()
    {
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.ValidateCertificates);
    }

    [Fact]
    public void AddConfigServer_XmlAppSettingsConfiguresClient()
    {
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.xml", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddXmlFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://foo.com:9999", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("myName", options.Name);
        Assert.Equal("myLabel", options.Label);
        Assert.Equal("myUsername", options.Username);
        Assert.Equal("myPassword", options.Password);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
    }

    [Fact]
    public void AddConfigServer_IniAppSettingsConfiguresClient()
    {
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.ini", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddIniFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://foo.com:9999", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("myName", options.Name);
        Assert.Equal("myLabel", options.Label);
        Assert.Equal("myUsername", options.Username);
        Assert.Equal("myPassword", options.Password);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
    }

    [Fact]
    public void AddConfigServer_CommandLineAppSettingsConfiguresClient()
    {
        string[] appsettings =
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
        configurationBuilder.AddCommandLine(appsettings);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://foo.com:9999", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("myName", options.Name);
        Assert.Equal("myLabel", options.Label);
        Assert.Equal("myUsername", options.Username);
        Assert.Equal("myPassword", options.Password);
        Assert.Null(options.AccessTokenUri);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
    }

    [Fact]
    public void AddConfigServer_HandlesPlaceHolders()
    {
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);

        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.False(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://user:password@foo.com:9999", options.Uri);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("testName", options.Name);
        Assert.Equal("myLabel", options.Label);
        Assert.Equal("myUsername", options.Username);
        Assert.Equal("myPassword", options.Password);
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

        const string appsettings = """
            {
              "spring": {
                "application": {
                  "name": "${vcap:application:name?foobar}"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string appsettingsPath = sandbox.CreateFile("appsettings.json", appsettings);
        string appSettingsFileName = Path.GetFileName(appsettingsPath);

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

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.True(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com", options.Uri);
        Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", options.AccessTokenUri);
        Assert.Equal("p-config-server-a74fc0a3-a7c3-43b6-81f9-9eb6586dd3ef", options.ClientId);
        Assert.Equal("e8KF1hXvAnGd", options.ClientSecret);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("my-app", options.Name);
        Assert.Null(options.Label);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
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

        const string appsettings = """
            {
              "spring": {
                "application": {
                  "name": "${vcap:application:name?foobar}"
                }
              }
            }
            """;

        using var sandbox = new Sandbox();
        string appSettingsPath = sandbox.CreateFile("appsettings.json", appsettings);
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

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().SingleOrDefault();
        Assert.NotNull(configServerProvider);

        ConfigServerClientOptions options = configServerProvider.ClientOptions;
        Assert.True(options.Enabled);
        Assert.False(options.FailFast);
        Assert.Equal("https://config-ba6b6079-163b-45d2-8932-e2eca0d1e49a.wise.com", options.Uri);
        Assert.Equal("https://p-spring-cloud-services.uaa.wise.com/oauth/token", options.AccessTokenUri);
        Assert.Equal("config-client-ea5e13c2-def2-4a3b-b80c-38e690ec284f", options.ClientId);
        Assert.Equal("e8KF1hXvAnGd", options.ClientSecret);
        Assert.Equal("Production", options.Environment);
        Assert.Equal("my-app", options.Name);
        Assert.Null(options.Label);
        Assert.Null(options.Username);
        Assert.Null(options.Password);
    }
}
