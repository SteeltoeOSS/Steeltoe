// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerClientOptionsTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var options = new ConfigServerClientOptions();

        TestHelper.VerifyDefaults(options, null);
    }

    [Fact]
    public async Task ConfigureConfigServerClientOptions_WithDefaults()
    {
        var builder = new ConfigurationBuilder();
        builder.AddConfigServer();
        IConfiguration configuration = builder.Build();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.ConfigureConfigServerClientOptions();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        string? expectedAppName = Assembly.GetEntryAssembly()!.GetName().Name;
        TestHelper.VerifyDefaults(optionsMonitor.CurrentValue, expectedAppName);
    }

    [Fact]
    public async Task ConfigureConfigServerClientOptions_WithValues()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddOptions();

        const string appSettings = """
            {
              "spring": {
                "application": {
                  "name": "foo"
                },
                "cloud": {
                  "config": {
                    "uri": "http://localhost:8888",
                    "env": "development",
                    "headers": {
                      "foo": "bar",
                      "bar": "foo"
                    },
                    "health": {
                      "enabled": true
                    },
                    "failFast": "true"
                  }
                }
              }
            }
            """;

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeAppSettingsJsonFile(appSettings);

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryAppSettingsJsonFile(fileProvider);
        IConfiguration configuration = builder.Build();
        services.AddSingleton(configuration);

        services.ConfigureConfigServerClientOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var service = serviceProvider.GetRequiredService<IOptions<ConfigServerClientOptions>>();

        ConfigServerClientOptions options = service.Value;

        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeTrue();
        options.Uri.Should().Be("http://localhost:8888");
        options.Environment.Should().Be("development");
        options.AccessTokenUri.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ValidateCertificates.Should().BeTrue();
        options.Retry.InitialInterval.Should().Be(1000);
        options.Retry.MaxAttempts.Should().Be(6);
        options.Retry.Enabled.Should().BeFalse();
        options.Retry.Multiplier.Should().Be(1.1);
        options.Retry.MaxInterval.Should().Be(2000);
        options.Timeout.Should().Be(60_000);
        options.TokenRenewRate.Should().Be(60_000);
        options.TokenTtl.Should().Be(300_000);
        options.Discovery.Enabled.Should().BeFalse();
        options.Discovery.ServiceId.Should().Be("configserver");
        options.Health.Enabled.Should().BeTrue();
        options.Health.TimeToLive.Should().Be(300_000);
        options.Name.Should().Be("foo");
        options.Label.Should().BeNull();
        options.Username.Should().BeNull();
        options.Password.Should().BeNull();
        options.Token.Should().BeNull();
        options.Headers.Should().HaveCount(2);
        options.Headers.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        options.Headers.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public void Clone_preserves_all_properties_and_produces_independent_nested_objects()
    {
        using var certificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");
        using var issuerCertificate = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");

        var original = new ConfigServerClientOptions
        {
            ClientCertificate =
            {
                Certificate = certificate,
                IssuerChain =
                {
                    issuerCertificate
                }
            },
            Enabled = false,
            FailFast = true,
            Environment = "staging",
            Label = "feature/x",
            Name = "my-app",
            Uri = "https://config.example.com:9999",
            Username = "user",
            Password = "pass",
            Token = "vault-token-123",
            Timeout = 42_000,
            PollingInterval = TimeSpan.FromSeconds(15),
            ValidateCertificates = false,
            Retry =
            {
                Enabled = true,
                InitialInterval = 500,
                MaxInterval = 5000,
                Multiplier = 2.0,
                MaxAttempts = 10
            },
            Discovery =
            {
                Enabled = true,
                ServiceId = "my-config-server"
            },
            Health =
            {
                Enabled = false,
                TimeToLive = 999
            },
            AccessTokenUri = "https://uaa.example.com/oauth/token",
            ClientSecret = "secret",
            ClientId = "client-id",
            TokenTtl = 600_000,
            TokenRenewRate = 120_000,
            DisableTokenRenewal = true,
            Headers =
            {
                ["X-Custom"] = "value"
            }
        };

        ConfigServerClientOptions clone = original.Clone();

        clone.ClientCertificate.Should().NotBeSameAs(original.ClientCertificate);
        clone.ClientCertificate.Certificate.Should().BeSameAs(original.ClientCertificate.Certificate);
        clone.ClientCertificate.IssuerChain.Should().NotBeSameAs(original.ClientCertificate.IssuerChain);
        clone.ClientCertificate.IssuerChain.Should().ContainSingle().Which.Should().BeSameAs(issuerCertificate);

        original.ClientCertificate.IssuerChain.Clear();
        clone.ClientCertificate.IssuerChain.Should().ContainSingle();

        clone.Enabled.Should().Be(original.Enabled);
        clone.FailFast.Should().Be(original.FailFast);
        clone.Environment.Should().Be(original.Environment);
        clone.Label.Should().Be(original.Label);
        clone.Name.Should().Be(original.Name);
        clone.Uri.Should().Be(original.Uri);
        clone.Username.Should().Be(original.Username);
        clone.Password.Should().Be(original.Password);
        clone.Token.Should().Be(original.Token);
        clone.Timeout.Should().Be(original.Timeout);
        clone.PollingInterval.Should().Be(original.PollingInterval);
        clone.ValidateCertificates.Should().Be(original.ValidateCertificates);

        clone.Retry.Should().NotBeSameAs(original.Retry);
        clone.Retry.Enabled.Should().Be(original.Retry.Enabled);
        clone.Retry.InitialInterval.Should().Be(original.Retry.InitialInterval);
        clone.Retry.MaxInterval.Should().Be(original.Retry.MaxInterval);
        clone.Retry.Multiplier.Should().Be(original.Retry.Multiplier);
        clone.Retry.MaxAttempts.Should().Be(original.Retry.MaxAttempts);

        clone.Discovery.Should().NotBeSameAs(original.Discovery);
        clone.Discovery.Enabled.Should().Be(original.Discovery.Enabled);
        clone.Discovery.ServiceId.Should().Be(original.Discovery.ServiceId);

        clone.Health.Should().NotBeSameAs(original.Health);
        clone.Health.Enabled.Should().Be(original.Health.Enabled);
        clone.Health.TimeToLive.Should().Be(original.Health.TimeToLive);

        clone.AccessTokenUri.Should().Be(original.AccessTokenUri);
        clone.ClientSecret.Should().Be(original.ClientSecret);
        clone.ClientId.Should().Be(original.ClientId);
        clone.TokenTtl.Should().Be(original.TokenTtl);
        clone.TokenRenewRate.Should().Be(original.TokenRenewRate);
        clone.DisableTokenRenewal.Should().Be(original.DisableTokenRenewal);

        clone.Headers.Should().NotBeSameAs(original.Headers);
        clone.Headers.Should().BeEquivalentTo(original.Headers);
    }

    [Fact]
    public void Certificate_configuration_survives_options_reload()
    {
        const string configServerResponseJson = """
            {
              "name": "myName",
              "profiles": [ "Production" ],
              "label": "test-label",
              "version": "test-version",
              "propertySources": []
            }
            """;

        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "timeout": 30000
                  }
                }
              },
              "Certificates": {
                "ConfigServer": {
                  "CertificateFilePath": "instance.crt",
                  "PrivateKeyFilePath": "instance.key"
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "http://localhost:8888/myName/Production").Respond("application/json", configServerResponseJson);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        handler.Mock.VerifyNoOutstandingExpectation();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.ConfigureConfigServerClientOptions();

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        provider.ClientOptions.ClientCertificate.Certificate.Should().NotBeNull();
        optionsMonitor.CurrentValue.ClientCertificate.Certificate.Should().NotBeNull();

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "name": "myName",
                    "timeout": 15000
                  }
                }
              },
              "Certificates": {
                "ConfigServer": {
                  "CertificateFilePath": "instance.crt",
                  "PrivateKeyFilePath": "instance.key"
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.ClientOptions.Timeout.Should().Be(15_000);
        provider.ClientOptions.ClientCertificate.Certificate.Should().NotBeNull();
        optionsMonitor.CurrentValue.Timeout.Should().Be(15_000);
        optionsMonitor.CurrentValue.ClientCertificate.Certificate.Should().NotBeNull();
    }

    [Fact]
    public void Changes_in_IConfiguration_update_provider_options_and_injected_options()
    {
        const string configServerResponseJson = """
            {
              "name": "example-app-name",
              "profiles": [
                "example-profile"
              ],
              "label": "example-label",
              "version": "1",
              "propertySources": [
                {
                  "name": "example-source",
                  "source": {
                    "example-server-key": "example-server-value"
                  }
                }
              ]
            }
            """;

        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "uri": "https://config.server.com:9999",
                    "name": "example-app-name",
                    "env": "example-profile",
                    "timeout": 30000
                  }
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://config.server.com:9999/example-app-name/example-profile/example-label")
            .Respond("application/json", configServerResponseJson);

        var initialOptions = new ConfigServerClientOptions
        {
            Name = "ignored-because-overruled-from-appsettings",
            Label = "example-label" // used, but missing in IConfiguration and injected options
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        configurationBuilder.AddConfigServer(initialOptions, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        handler.Mock.VerifyNoOutstandingExpectation();
        handler.Mock.Clear();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.ConfigureConfigServerClientOptions();

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        provider.ClientOptions.Uri.Should().Be("https://config.server.com:9999");
        provider.ClientOptions.Name.Should().Be("example-app-name");
        provider.ClientOptions.Environment.Should().Be("example-profile");
        provider.ClientOptions.Timeout.Should().Be(30_000);
        provider.ClientOptions.Label.Should().Be("example-label");

        optionsMonitor.CurrentValue.Uri.Should().Be(provider.ClientOptions.Uri);
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Timeout.Should().Be(provider.ClientOptions.Timeout);
        optionsMonitor.CurrentValue.Label.Should().BeNull();

        configuration["example-server-key"].Should().Be("example-server-value");

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "uri": "https://alternate-config.server.com:7777",
                    "name": "alternate-name",
                    "env": "example-profile",
                    "timeout": 15000,
                    "label": "alternate-label"
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        AssertFinal();

        handler.Mock.Expect(HttpMethod.Get, "https://alternate-config.server.com:7777/alternate-name/example-profile/alternate-label")
            .Respond("application/json", configServerResponseJson);

        provider.Load();
        handler.Mock.VerifyNoOutstandingExpectation();

        AssertFinal();

        void AssertFinal()
        {
            provider.ClientOptions.Uri.Should().Be("https://alternate-config.server.com:7777");
            provider.ClientOptions.Name.Should().Be("alternate-name");
            provider.ClientOptions.Environment.Should().Be("example-profile");
            provider.ClientOptions.Timeout.Should().Be(15_000);
            provider.ClientOptions.Label.Should().Be("alternate-label");

            optionsMonitor.CurrentValue.Uri.Should().Be(provider.ClientOptions.Uri);
            optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
            optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
            optionsMonitor.CurrentValue.Timeout.Should().Be(provider.ClientOptions.Timeout);
            optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);

            configuration["example-server-key"].Should().Be("example-server-value");
        }
    }
}
