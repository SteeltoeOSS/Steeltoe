// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
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
        configurationBuilder.AddConfigServer(options);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();
        provider.ClientOptions.ClientCertificate.Certificate.Should().NotBeNull();
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
        configurationBuilder.AddConfigServer(options);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().Single();
        provider.ClientOptions.ClientCertificate.Certificate.Should().NotBeNull();
    }

    [Fact]
    public void AddConfigServer_AddsConfigServerSourceToList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddConfigServer();

        ConfigServerConfigurationSource? source = configurationBuilder.EnumerateSources<ConfigServerConfigurationSource>().SingleOrDefault();
        source.Should().NotBeNull();
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
        provider.ClientOptions.ClientId.Should().Be("some-client-id");
        provider.ClientOptions.ClientSecret.Should().Be("some-secret");
        provider.ClientOptions.AccessTokenUri.Should().Be("https://uaa-uri-from-vcap-services/oauth/token");
    }

    [Fact]
    public void AddConfigServer_CallbackOverridesConfigurationOverridesDefaultOptions()
    {
        var options = new ConfigServerClientOptions
        {
            Name = "nameInOptions",
            Label = "labelInOptions",
            Environment = "environmentInOptions",
            Username = "usernameInOptions",
            Password = "passwordInOptions",
            Timeout = 10,
            Retry =
            {
                InitialInterval = 5,
                MaxInterval = 15,
                MaxAttempts = 12
            }
        };

        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "Spring": {
                "Cloud": {
                  "Config": {
                    "Name": "nameInAppSettings",
                    "Label": "labelInAppSettings",
                    "Timeout": 50,
                    "Retry": {
                      "MaxInterval": 100,
                      "MaxAttempts": 9
                    }
                  }
                }
              }
            }
            """);

        Action<ConfigServerClientOptions> configureOptions = clientOptions => clientOptions.Retry.MaxAttempts = 2;

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(options, configureOptions, null, NullLoggerFactory.Instance);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        ConfigServerConfigurationProvider? provider = configurationRoot.EnumerateProviders<ConfigServerConfigurationProvider>().FirstOrDefault();

        provider.Should().NotBeNull();
        provider.ClientOptions.Name.Should().Be("nameInAppSettings");
        provider.ClientOptions.Label.Should().Be("labelInAppSettings");
        provider.ClientOptions.Environment.Should().Be("environmentInOptions");
        provider.ClientOptions.Username.Should().Be("usernameInOptions");
        provider.ClientOptions.Password.Should().Be("passwordInOptions");
        provider.ClientOptions.Timeout.Should().Be(50);
        provider.ClientOptions.Retry.InitialInterval.Should().Be(5);
        provider.ClientOptions.Retry.MaxInterval.Should().Be(100);
        provider.ClientOptions.Retry.MaxAttempts.Should().Be(2);

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "Spring": {
                "Cloud": {
                  "Config": {
                    "Name": "alternateNameInAppSettings",
                    "Username": "alternateUsernameInAppSettings"
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.ClientOptions.Name.Should().Be("alternateNameInAppSettings");
        provider.ClientOptions.Label.Should().Be("labelInOptions");
        provider.ClientOptions.Environment.Should().Be("environmentInOptions");
        provider.ClientOptions.Username.Should().Be("alternateUsernameInAppSettings");
        provider.ClientOptions.Password.Should().Be("passwordInOptions");
        provider.ClientOptions.Timeout.Should().Be(10);
        provider.ClientOptions.Retry.InitialInterval.Should().Be(5);
        provider.ClientOptions.Retry.MaxInterval.Should().Be(15);
        provider.ClientOptions.Retry.MaxAttempts.Should().Be(2);
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
