// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;

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

        ConfigServerConfigurationSource? source = configurationBuilder.EnumerateSources<ConfigServerConfigurationSource>().SingleOrDefault();
        source.Should().NotBeNull();
        source.DefaultOptions.ClientCertificate.Should().NotBeNull();
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

        ConfigServerConfigurationSource? source = configurationBuilder.EnumerateSources<ConfigServerConfigurationSource>().SingleOrDefault();
        source.Should().NotBeNull();
        source.DefaultOptions.ClientCertificate.Should().NotBeNull();
    }

    [Fact]
    public void AddConfigServer_AddsConfigServerSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer();

        ConfigServerConfigurationSource? source = configurationBuilder.EnumerateSources<ConfigServerConfigurationSource>().SingleOrDefault();
        source.Should().NotBeNull();
    }

    [Fact]
    public void AddConfigServer_WithLoggerFactorySucceeds()
    {
        CapturingLoggerProvider loggerProvider = new();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer(loggerFactory);
        _ = configurationBuilder.Build();

        IList<string> logMessages = loggerProvider.GetAll();

        logMessages.Should().Contain(
            "DBUG Steeltoe.Configuration.ConfigServer.ConfigServerConfigurationProvider: Fetching configuration from server(s).");
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

        ConfigServerConfigurationProvider? provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().FirstOrDefault();

        provider.Should().BeOfType<ConfigServerConfigurationProvider>();
        provider.ClientOptions.Uri.Should().NotBe("https://uri-from-settings");
        provider.ClientOptions.Uri.Should().Be("https://uri-from-vcap-services");
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

        ConfigServerConfigurationProvider? provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().FirstOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Label.Should().Be("testConfigLabel");
        provider.ClientOptions.Name.Should().Be("testConfigName");
        provider.ClientOptions.Environment.Should().Be("testEnv");
        provider.ClientOptions.Username.Should().Be("testUser");
        provider.ClientOptions.Password.Should().Be("testPassword");
    }

    [Fact]
    public void AddConfigServer_AddsCloudFoundryConfigurationSource()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer();

        configurationBuilder.EnumerateSources<CloudFoundryConfigurationSource>().Should().ContainSingle();
    }

    [Fact]
    public void AddConfigServer_Only_AddsOneCloudFoundryConfigurationSource()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry(new OtherCloudFoundrySettingsReader());
        configurationBuilder.AddConfigServer();

        configurationBuilder.EnumerateSources<CloudFoundryConfigurationSource>().Should().ContainSingle();
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
