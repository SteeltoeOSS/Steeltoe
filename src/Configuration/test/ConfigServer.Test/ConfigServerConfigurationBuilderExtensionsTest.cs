// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerConfigurationBuilderExtensionsTest
{
    private const string VcapApplication = """
        {
            "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
            "application_name": "foo",
            "application_uris": [
                "foo.10.244.0.34.xip.io"
            ],
            "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
            "limits": {
                "disk": 1024,
                "fds": 16384,
                "mem": 256
            },
            "name": "foo",
            "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
            "space_name": "my-space",
            "uris": [
                "foo.10.244.0.34.xip.io"
            ],
            "users": null,
            "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
        }
        """;

    private const string VcapServicesV2 = """
        {
            "p-config-server": [
            {
                "name": "config-server",
                "instance_name": "config-server",
                "binding_name": null,
                "credentials": {
                    "uri": "https://uri-from-vcap-services",
                    "client_secret": "some-secret",
                    "client_id": "some-client-id",
                    "access_token_uri": "https://uaa-uri-from-vcap-services/oauth/token"
                },
                "syslog_drain_url": null,
                "volume_mounts": [],
                "label": "p-config-server",
                "plan": "standard",
                "provider": null,
                "tags": [
                    "configuration",
                    "spring-cloud"
                ]
            }]
        }
        """;

    private const string VcapServicesV3 = """
        {
            "p.config-server": [
            {
                "name": "config-server",
                "instance_name": "config-server",
                "binding_name": null,
                "credentials": {
                    "uri": "https://uri-from-vcap-services",
                    "client_secret": "some-secret",
                    "client_id": "some-client-id",
                    "access_token_uri": "https://uaa-uri-from-vcap-services/oauth/token"
                },
                "syslog_drain_url": null,
                "volume_mounts": [],
                "label": "p-config-server",
                "plan": "standard",
                "provider": null,
                "tags": [
                    "configuration",
                    "spring-cloud"
                ]
            }]
        }
        """;

    private const string VcapServicesAlt = """
        {
            "config-server": [
            {
                "name": "config-server",
                "instance_name": "config-server",
                "binding_name": null,
                "credentials": {
                    "uri": "https://uri-from-vcap-services",
                    "client_secret": "some-secret",
                    "client_id": "some-client-id",
                    "access_token_uri": "https://uaa-uri-from-vcap-services/oauth/token"
                },
                "syslog_drain_url": null,
                "volume_mounts": [],
                "label": "p-config-server",
                "plan": "standard",
                "provider": null,
                "tags": [
                    "configuration",
                    "spring-cloud"
                ]
            }]
        }
        """;

    [Fact]
    public void AddConfigServer_WithConfigServerCertificate_AddsConfigServerSourceWithCertificate()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Certificates:ConfigServer:CertificateFilePath"] = "instance.crt",
            ["Certificates:ConfigServer:PrivateKeyFilePath"] = "instance.key"
        };

        var options = new ConfigServerClientOptions
        {
            Timeout = 10
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        configurationBuilder.AddConfigServer(options, NullLoggerFactory.Instance);
        _ = configurationBuilder.Build();

        var source = configurationBuilder.FindConfigurationSource<ConfigServerConfigurationSource>();
        Assert.NotNull(source);
        Assert.NotNull(source.DefaultOptions.ClientCertificate);
    }

    [Fact]
    public void AddConfigServer_WithGlobalCertificate_AddsConfigServerSourceWithCertificate()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Certificates:CertificateFilePath"] = "instance.crt",
            ["Certificates:PrivateKeyFilePath"] = "instance.key"
        };

        var options = new ConfigServerClientOptions
        {
            Timeout = 10
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        configurationBuilder.AddConfigServer(options, NullLoggerFactory.Instance);
        _ = configurationBuilder.Build();

        var source = configurationBuilder.FindConfigurationSource<ConfigServerConfigurationSource>();
        Assert.NotNull(source);
        Assert.NotNull(source.DefaultOptions.ClientCertificate);
    }

    [Fact]
    public void AddConfigServer_AddsConfigServerSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer();

        ConfigServerConfigurationSource? configServerSource = configurationBuilder.Sources.OfType<ConfigServerConfigurationSource>().SingleOrDefault();
        Assert.NotNull(configServerSource);
    }

    [Fact]
    public void AddConfigServer_WithLoggerFactorySucceeds()
    {
        CapturingLoggerProvider loggerProvider = new();
        var loggerFactory = new LoggerFactory([loggerProvider]);

        var configurationBuilder = new ConfigurationBuilder();
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
                        "token" : "vaulttoken",
                        "retry": {
                            "enabled":"false",
                            "initialInterval":55555,
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddXmlFile(fileName);
        configurationBuilder.AddConfigServer();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? configServerProvider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
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
    }

    [Fact]
    public void AddConfigServer_HandlesPlaceHolders()
    {
        const string appsettings = """
            {
                "foo": {
                    "bar": {
                        "name": "testName"
                    },
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

    [Theory]
    [InlineData(VcapServicesV2)]
    [InlineData(VcapServicesV3)]
    [InlineData(VcapServicesAlt)]
    public void AddConfigServer_VCAP_SERVICES_Override_Defaults(string vcapServices)
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);

        var options = new ConfigServerClientOptions
        {
            Uri = "https://uri-from-settings",
            Retry =
            {
                Enabled = false
            },
            Timeout = 10,
            Enabled = false
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddConfigServer(options, NullLoggerFactory.Instance);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();

        Assert.NotNull(provider);
        Assert.IsType<ConfigServerConfigurationProvider>(provider);
        Assert.NotEqual("https://uri-from-settings", provider.ClientOptions.Uri);
        Assert.Equal("https://uri-from-vcap-services", provider.ClientOptions.Uri);
    }

    [Fact]
    public void AddConfigServer_PaysAttentionToSettings()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "testConfigName",
            Label = "testConfigLabel",
            Environment = "testEnv",
            Username = "testUser",
            Password = "testPassword",
            Timeout = 10,
            Retry =
            {
                Enabled = false
            }
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer(options, NullLoggerFactory.Instance);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? provider = configurationRoot.Providers.OfType<ConfigServerConfigurationProvider>().FirstOrDefault();

        Assert.NotNull(provider);
        Assert.Equal("testConfigLabel", provider.ClientOptions.Label);
        Assert.Equal("testConfigName", provider.ClientOptions.Name);
        Assert.Equal("testEnv", provider.ClientOptions.Environment);
        Assert.Equal("testUser", provider.ClientOptions.Username);
        Assert.Equal("testPassword", provider.ClientOptions.Password);
    }

    [Fact]
    public void AddConfigServer_AddsCloudFoundryConfigurationSource()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer();

        var source = configurationBuilder.FindConfigurationSource<CloudFoundryConfigurationSource>();
        Assert.NotNull(source);
    }

    [Fact]
    public void AddConfigServer_Only_AddsOneCloudFoundryConfigurationSource()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry(new OtherCloudFoundrySettingsReader());
        configurationBuilder.AddConfigServer();

        Assert.Single(configurationBuilder.GetConfigurationSources<CloudFoundryConfigurationSource>());
    }

    private sealed class OtherCloudFoundrySettingsReader : ICloudFoundrySettingsReader
    {
        public string ApplicationJson => throw new NotImplementedException();
        public string InstanceId => throw new NotImplementedException();
        public string InstanceIndex => throw new NotImplementedException();
        public string InstanceInternalIP => throw new NotImplementedException();
        public string InstanceIP => throw new NotImplementedException();
        public string InstancePort => throw new NotImplementedException();
        public string ServicesJson => throw new NotImplementedException();
    }
}
