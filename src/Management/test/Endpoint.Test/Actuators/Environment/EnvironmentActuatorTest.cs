// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Environment;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Environment;

public sealed class EnvironmentActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "env"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IWebHostEnvironment, FakeWebHostEnvironment>();
        services.AddEnvironmentActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<EnvironmentEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddEnvironmentActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        EnvironmentEndpointOptions options = serviceProvider.GetRequiredService<IOptions<EnvironmentEndpointOptions>>().Value;

        options.KeysToSanitize.Should().BeEquivalentTo("password", "secret", "key", "token", ".*credentials.*", "vcap_services");
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("env");
        options.Path.Should().Be("env");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/env");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Env:KeysToSanitize:0"] = "accessToken",
            ["Management:Endpoints:Env:KeysToSanitize:1"] = "secureKey",
            ["Management:Endpoints:Env:Enabled"] = "true",
            ["Management:Endpoints:Env:Id"] = "test-actuator-id",
            ["Management:Endpoints:Env:Path"] = "test-actuator-path",
            ["Management:Endpoints:Env:RequiredPermissions"] = "full",
            ["Management:Endpoints:Env:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddEnvironmentActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        EnvironmentEndpointOptions options = serviceProvider.GetRequiredService<IOptions<EnvironmentEndpointOptions>>().Value;

        options.KeysToSanitize.Should().BeEquivalentTo("accessToken", "secureKey");
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Fact]
    public async Task Can_clear_default_keys_to_sanitize()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Env:KeysToSanitize:0"] = string.Empty
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddEnvironmentActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        EnvironmentEndpointOptions options = serviceProvider.GetRequiredService<IOptions<EnvironmentEndpointOptions>>().Value;

        options.KeysToSanitize.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Steeltoe"] = "Information",
            ["Logging:LogLevel:TestApp"] = "Error",
            ["Do:Not:Show:This:Secret"] = "hidden-in-response"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddInMemoryCollection(appSettings);
            });

            builder.ConfigureServices(services => services.AddEnvironmentActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Do:Not:Show:This:Secret": {
                      "value": "******"
                    },
                    "Logging:LogLevel:Default": {
                      "value": "Warning"
                    },
                    "Logging:LogLevel:Steeltoe": {
                      "value": "Information"
                    },
                    "Logging:LogLevel:TestApp": {
                      "value": "Error"
                    },
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Only_shows_changed_values_from_placeholder_resolver()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Test:ReplacementValue"] = "evaluated-outcome",
            ["Test:Placeholder"] = "Result = ${Test:ReplacementValue}"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Configuration.AddPlaceholderResolver();
        builder.Services.AddEnvironmentActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    },
                    "Test:Placeholder": {
                      "value": "Result = ${Test:ReplacementValue}"
                    },
                    "Test:ReplacementValue": {
                      "value": "evaluated-outcome"
                    }
                  }
                },
                {
                  "name": "PlaceholderConfigurationProvider",
                  "properties": {
                    "Test:Placeholder": {
                      "value": "Result = evaluated-outcome"
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Includes_sources_without_any_keys()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection([]);
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddInMemoryCollection([]);
        builder.Services.AddEnvironmentActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "activeProfiles": [
                "Test"
              ],
              "propertySources": [
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {}
                },
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    }
                  }
                },
                {
                  "name": "MemoryConfigurationProvider",
                  "properties": {}
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Can_change_configuration_at_runtime()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Management": {
            "Endpoints": {
              "Actuator": {
                "Exposure": {
                  "Include": [
                    "env"
                  ]
                }
              },
              "Env": {
                "KeysToSanitize": [
                  "Password"
                ]
              }
            }
          },
          "TestSettings": {
            "Password": "secret-password",
            "AccessToken": "secret-token"
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddEnvironmentActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "JsonConfigurationProvider: [appsettings.json]",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    },
                    "Management:Endpoints:Env:KeysToSanitize:0": {
                      "value": "Password"
                    },
                    "TestSettings:AccessToken": {
                      "value": "secret-token"
                    },
                    "TestSettings:Password": {
                      "value": "******"
                    }
                  }
                }
              ]
            }
            """);

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Management": {
            "Endpoints": {
              "Actuator": {
                "Exposure": {
                  "Include": [
                    "env"
                  ]
                }
              },
              "Env": {
                "KeysToSanitize": [
                  "AccessToken"
                ]
              }
            }
          },
          "TestSettings": {
            "Password": "secret-password",
            "AccessToken": "secret-token"
          }
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/env"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "activeProfiles": [
                "Production"
              ],
              "propertySources": [
                {
                  "name": "JsonConfigurationProvider: [appsettings.json]",
                  "properties": {
                    "Management:Endpoints:Actuator:Exposure:Include:0": {
                      "value": "env"
                    },
                    "Management:Endpoints:Env:KeysToSanitize:0": {
                      "value": "AccessToken"
                    },
                    "TestSettings:AccessToken": {
                      "value": "******"
                    },
                    "TestSettings:Password": {
                      "value": "secret-password"
                    }
                  }
                }
              ]
            }
            """);
    }
}
