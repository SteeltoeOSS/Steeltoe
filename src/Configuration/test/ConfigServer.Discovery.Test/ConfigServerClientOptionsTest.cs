// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Discovery.Test;

public sealed class ConfigServerClientOptionsTest
{
    [Fact]
    public void Config_Server_URI_is_resolved_from_discovery_and_survives_changes_in_IConfiguration()
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
                    "discovery": {
                      "enabled": true
                    },
                    "uri": "http://overruled-by-discovery",
                    "name": "example-app-name",
                    "env": "example-profile",
                    "timeout": 30000,
                    "label": "example-label"
                  }
                }
              },
              "discovery": {
                "services": [
                  {
                    "serviceId": "configserver",
                    "host": "discovered-server.com",
                    "port": 9999,
                    "isSecure": true,
                    "metadata": {
                      "user": "example-user",
                      "password": "example-password",
                      "configPath": "internal"
                    }
                  }
                ]
              },
              "eureka": {
                "client": {
                  "enabled": false
                }
              },
              "consul": {
                "discovery": {
                  "enabled": false
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://discovered-server.com:9999/internal/example-app-name/example-profile/example-label")
            .Respond("application/json", configServerResponseJson);

        Action<ConfigServerClientOptions> configureOptions = options => options.ValidateCertificates = false;

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        // ReSharper disable once AccessToDisposedClosure
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), configureOptions, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        handler.Mock.VerifyNoOutstandingExpectation();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.ConfigureConfigServerClientOptions(configureOptions);

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        provider.ClientOptions.Uri.Should().Be("http://overruled-by-discovery");
        provider.ClientOptions.Username.Should().BeNull();
        provider.ClientOptions.Password.Should().BeNull();
        provider.ClientOptions.Name.Should().Be("example-app-name");
        provider.ClientOptions.Environment.Should().Be("example-profile");
        provider.ClientOptions.Timeout.Should().Be(30_000);
        provider.ClientOptions.Label.Should().Be("example-label");
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        optionsMonitor.CurrentValue.Uri.Should().Be("https://discovered-server.com:9999/internal");
        optionsMonitor.CurrentValue.Username.Should().Be("example-user");
        optionsMonitor.CurrentValue.Password.Should().Be("example-password");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Timeout.Should().Be(provider.ClientOptions.Timeout);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "discovery": {
                      "enabled": true
                    },
                    "uri": "http://overruled-by-discovery",
                    "name": "alternate-name-1",
                    "env": "example-profile",
                    "timeout": 15000,
                    "label": "example-label"
                  }
                }
              },
              "discovery": {
                "services": [
                  {
                    "serviceId": "configserver",
                    "host": "ignored-other-discovered-server.com",
                    "port": 3333,
                    "isSecure": true,
                    "metadata": {
                      "user": "ignored-other-example-user",
                      "password": "ignored-other-example-password",
                      "configPath": "ignored-other-internal"
                    }
                  }
                ]
              },
              "eureka": {
                "client": {
                  "enabled": false
                }
              },
              "consul": {
                "discovery": {
                  "enabled": false
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.ClientOptions.Uri.Should().Be("http://overruled-by-discovery");
        provider.ClientOptions.Username.Should().BeNull();
        provider.ClientOptions.Password.Should().BeNull();
        provider.ClientOptions.Name.Should().Be("alternate-name-1");
        provider.ClientOptions.Environment.Should().Be("example-profile");
        provider.ClientOptions.Timeout.Should().Be(15_000);
        provider.ClientOptions.Label.Should().Be("example-label");
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        // Discovery changes don't propagate until the provider reloads.
        optionsMonitor.CurrentValue.Uri.Should().Be("https://discovered-server.com:9999/internal");
        optionsMonitor.CurrentValue.Username.Should().Be("example-user");
        optionsMonitor.CurrentValue.Password.Should().Be("example-password");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Timeout.Should().Be(provider.ClientOptions.Timeout);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "discovery": {
                      "enabled": false
                    },
                    "uri": "https://explicit-server:7777",
                    "name": "alternate-name-2",
                    "env": "example-profile",
                    "timeout": 10000
                  }
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.ClientOptions.Uri.Should().Be("https://explicit-server:7777");
        provider.ClientOptions.Name.Should().Be("alternate-name-2");
        provider.ClientOptions.Environment.Should().Be("example-profile");
        provider.ClientOptions.Timeout.Should().Be(10_000);
        provider.ClientOptions.Label.Should().BeNull();
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        // Discovery changes don't propagate until the provider reloads.
        optionsMonitor.CurrentValue.Uri.Should().Be("https://discovered-server.com:9999/internal");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Timeout.Should().Be(provider.ClientOptions.Timeout);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");
    }

    [Fact]
    public void Updates_discovered_Config_Server_URI_on_provider_reload()
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
                    "discovery": {
                      "enabled": true
                    },
                    "uri": "http://overruled-by-discovery",
                    "name": "example-app-name",
                    "env": "example-profile",
                    "label": "example-label"
                  }
                }
              },
              "discovery": {
                "services": [
                  {
                    "serviceId": "configserver",
                    "host": "discovered-server.com",
                    "port": 9999,
                    "isSecure": true,
                    "metadata": {
                      "user": "example-user",
                      "password": "example-password",
                      "configPath": "internal"
                    }
                  }
                ]
              },
              "eureka": {
                "client": {
                  "enabled": false
                }
              },
              "consul": {
                "discovery": {
                  "enabled": false
                }
              }
            }
            """);

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Get, "https://discovered-server.com:9999/internal/example-app-name/example-profile/example-label")
            .Respond("application/json", configServerResponseJson);

        Action<ConfigServerClientOptions> configureOptions = options => options.ValidateCertificates = false;

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
        // ReSharper disable once AccessToDisposedClosure
        configurationBuilder.AddConfigServer(new ConfigServerClientOptions(), configureOptions, () => handler, NullLoggerFactory.Instance);
        IConfigurationRoot configuration = configurationBuilder.Build();

        handler.Mock.VerifyNoOutstandingExpectation();
        handler.Mock.Clear();

        ConfigServerConfigurationProvider provider = configuration.Providers.OfType<ConfigServerConfigurationProvider>().Single();

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.ConfigureConfigServerClientOptions(configureOptions);

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConfigServerClientOptions>>();

        provider.ClientOptions.Uri.Should().Be("http://overruled-by-discovery");
        provider.ClientOptions.Username.Should().BeNull();
        provider.ClientOptions.Password.Should().BeNull();
        provider.ClientOptions.Name.Should().Be("example-app-name");
        provider.ClientOptions.Environment.Should().Be("example-profile");
        provider.ClientOptions.Label.Should().Be("example-label");
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        optionsMonitor.CurrentValue.Uri.Should().Be("https://discovered-server.com:9999/internal");
        optionsMonitor.CurrentValue.Username.Should().Be("example-user");
        optionsMonitor.CurrentValue.Password.Should().Be("example-password");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "spring": {
                "cloud": {
                  "config": {
                    "discovery": {
                      "enabled": true
                    },
                    "uri": "http://overruled-again-by-discovery",
                    "name": "alternate-name",
                    "env": "alternate-profile",
                    "label": "alternate-label"
                  }
                }
              },
              "discovery": {
                "services": [
                  {
                    "serviceId": "configserver",
                    "host": "alternate-discovered-server.com",
                    "port": 7777,
                    "isSecure": true,
                    "metadata": {
                      "user": "alternate-user",
                      "password": "alternate-password",
                      "configPath": "internal"
                    }
                  }
                ]
              },
              "eureka": {
                "client": {
                  "enabled": false
                }
              },
              "consul": {
                "discovery": {
                  "enabled": false
                }
              }
            }
            """);

        fileProvider.NotifyChanged();

        provider.ClientOptions.Uri.Should().Be("http://overruled-again-by-discovery");
        provider.ClientOptions.Username.Should().BeNull();
        provider.ClientOptions.Password.Should().BeNull();
        provider.ClientOptions.Name.Should().Be("alternate-name");
        provider.ClientOptions.Environment.Should().Be("alternate-profile");
        provider.ClientOptions.Label.Should().Be("alternate-label");
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        optionsMonitor.CurrentValue.Uri.Should().Be("https://discovered-server.com:9999/internal");
        optionsMonitor.CurrentValue.Username.Should().Be("example-user");
        optionsMonitor.CurrentValue.Password.Should().Be("example-password");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");

        handler.Mock.Expect(HttpMethod.Get, "https://alternate-discovered-server.com:7777/internal/alternate-name/alternate-profile/alternate-label")
            .Respond("application/json", configServerResponseJson);

        provider.Load();
        handler.Mock.VerifyNoOutstandingExpectation();

        provider.ClientOptions.Uri.Should().Be("http://overruled-again-by-discovery");
        provider.ClientOptions.Username.Should().BeNull();
        provider.ClientOptions.Password.Should().BeNull();
        provider.ClientOptions.Name.Should().Be("alternate-name");
        provider.ClientOptions.Environment.Should().Be("alternate-profile");
        provider.ClientOptions.Label.Should().Be("alternate-label");
        provider.ClientOptions.ValidateCertificates.Should().BeFalse();

        optionsMonitor.CurrentValue.Uri.Should().Be("https://alternate-discovered-server.com:7777/internal");
        optionsMonitor.CurrentValue.Username.Should().Be("alternate-user");
        optionsMonitor.CurrentValue.Password.Should().Be("alternate-password");
        optionsMonitor.CurrentValue.Name.Should().Be(provider.ClientOptions.Name);
        optionsMonitor.CurrentValue.Environment.Should().Be(provider.ClientOptions.Environment);
        optionsMonitor.CurrentValue.Label.Should().Be(provider.ClientOptions.Label);
        optionsMonitor.CurrentValue.ValidateCertificates.Should().BeFalse();

        configuration["example-server-key"].Should().Be("example-server-value");
    }
}
