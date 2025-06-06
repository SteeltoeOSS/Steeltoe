// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersActuatorTest
{
    private static readonly MediaTypeHeaderValue RequestContentType = MediaTypeHeaderValue.Parse("application/vnd.spring-boot.actuator.v3+json");

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "loggers"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLoggersActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<LoggersEndpointMiddleware> action = serviceProvider.GetRequiredService<LoggersEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        LoggersEndpointOptions options = host.Services.GetRequiredService<IOptions<LoggersEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
        options.Id.Should().Be("loggers");
        options.Path.Should().Be("loggers");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().BeEquivalentTo("GET", "POST");
        options.RequiresExactMatch().Should().BeFalse();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/loggers/{**_}");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Loggers:Enabled"] = "true",
            ["Management:Endpoints:Loggers:Id"] = "test-actuator-id",
            ["Management:Endpoints:Loggers:Path"] = "test-actuator-path",
            ["Management:Endpoints:Loggers:RequiredPermissions"] = "full",
            ["Management:Endpoints:Loggers:AllowedVerbs:0"] = "post"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        LoggersEndpointOptions options = host.Services.GetRequiredService<IOptions<LoggersEndpointOptions>>().Value;

        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeFalse();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path/{**_}");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Logging:LogLevel:Default"] = "Error",
            ["Logging:LogLevel:Fake"] = "Warning",
            ["Logging:LogLevel:Fake.Category.AtDebugLevel"] = "Debug"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));
            builder.ConfigureLogging((context, loggingBuilder) => EnsureLoggingConfigurationIsBound(loggingBuilder, context.Configuration));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
                services.AddLoggersActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        response1.Content.Headers.ContentType.Should().NotBeNull();
        response1.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "effectiveLevel": "ERROR"
                }
              },
              "groups": {}
            }
            """);

        using var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        _ = loggerFactory.CreateLogger("Fake.Some");
        _ = loggerFactory.CreateLogger("Fake.Category.AtDebugLevel.Some");

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "effectiveLevel": "ERROR"
                },
                "Fake": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category.AtDebugLevel": {
                  "effectiveLevel": "DEBUG"
                },
                "Fake.Category.AtDebugLevel.Some": {
                  "effectiveLevel": "DEBUG"
                },
                "Fake.Some": {
                  "effectiveLevel": "WARN"
                }
              },
              "groups": {}
            }
            """);
    }

    [Fact]
    public async Task Can_change_minimum_levels()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Logging:LogLevel:Default"] = "Error",
            ["Logging:LogLevel:Fake"] = "Warning",
            ["Logging:LogLevel:Fake.Category.AtDebugLevel"] = "Debug"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        EnsureLoggingConfigurationIsBound(builder.Logging, builder.Configuration);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        using var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        _ = loggerFactory.CreateLogger("Fake.Some.Test");
        _ = loggerFactory.CreateLogger("Fake.Category.AtDebugLevel.Some");

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage setResponse1 = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), new StringContent("""
            {
                "configuredLevel": "OFF"
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        setResponse1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await setResponse1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();

        HttpResponseMessage setResponse2 = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Fake.Some.Test"), new StringContent("""
            {
                "configuredLevel": "INFO"
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        setResponse2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await setResponse2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();

        HttpResponseMessage setResponse3 = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Fake.Category"), new StringContent("""
            {
                "configuredLevel": "TRACE"
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        setResponse3.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await setResponse3.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();

        HttpResponseMessage getResponse1 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        getResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        string getResponseBody1 = await getResponse1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        getResponseBody1.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "configuredLevel": "ERROR",
                  "effectiveLevel": "OFF"
                },
                "Fake": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Category": {
                  "configuredLevel": "WARN",
                  "effectiveLevel": "TRACE"
                },
                "Fake.Category.AtDebugLevel": {
                  "effectiveLevel": "TRACE"
                },
                "Fake.Category.AtDebugLevel.Some": {
                  "effectiveLevel": "TRACE"
                },
                "Fake.Some": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Some.Test": {
                  "configuredLevel": "WARN",
                  "effectiveLevel": "INFO"
                }
              },
              "groups": {}
            }
            """);

        HttpResponseMessage resetResponse = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Fake.Category"), new StringContent("""
            {
                "configuredLevel": null
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await resetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();

        HttpResponseMessage getResponse2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        getResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        string getResponseBody2 = await getResponse2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        getResponseBody2.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "configuredLevel": "ERROR",
                  "effectiveLevel": "OFF"
                },
                "Fake": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Category": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Category.AtDebugLevel": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Category.AtDebugLevel.Some": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Some": {
                  "effectiveLevel": "OFF"
                },
                "Fake.Some.Test": {
                  "configuredLevel": "WARN",
                  "effectiveLevel": "INFO"
                }
              },
              "groups": {}
            }
            """);
    }

    [Fact]
    public async Task Fails_on_invalid_request_body()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), new StringContent("""
            {
                "configuredLevel": "BAD-LEVEL"
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task Ignores_other_logger_providers()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:Debug:LogLevel:Fake"] = "Debug",
            ["Logging:Console:LogLevel:Fake"] = "Information"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Logging.AddDebug();
        EnsureLoggingConfigurationIsBound(builder.Logging, builder.Configuration);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        using var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        _ = loggerFactory.CreateLogger("Fake.Some");

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage getResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string getResponseBody = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        getResponseBody.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "effectiveLevel": "WARN"
                },
                "Fake": {
                  "effectiveLevel": "INFO"
                },
                "Fake.Some": {
                  "effectiveLevel": "INFO"
                }
              },
              "groups": {}
            }
            """);
    }

    [Fact]
    public async Task Can_use_slash_as_management_path()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:Path"] = "/"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        EnsureLoggingConfigurationIsBound(builder.Logging, builder.Configuration);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage setResponse = await httpClient.PostAsync(new Uri("http://localhost/loggers/Default"), new StringContent("""
            {
                "configuredLevel": "ERROR"
            }
            """, RequestContentType), TestContext.Current.CancellationToken);

        setResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await setResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();

        HttpResponseMessage getResponse = await httpClient.GetAsync(new Uri("http://localhost/loggers"), TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string getResponseBody = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        getResponseBody.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "configuredLevel": "INFO",
                  "effectiveLevel": "ERROR"
                }
              },
              "groups": {}
            }
            """);
    }

    [Fact]
    public async Task Can_change_configuration_at_runtime()
    {
        var fileProvider = new MemoryFileProvider();
        const string appSettingsJsonFileName = "appsettings.json";

        fileProvider.IncludeFile(appSettingsJsonFileName, """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Error",
              "Fake": "Warning",
              "Fake.Category.AtDebugLevel": "Debug"
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        EnsureLoggingConfigurationIsBound(builder.Logging, builder.Configuration);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Services.AddLoggersActuator();
        WebApplication host = builder.Build();

        using var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        _ = loggerFactory.CreateLogger("Fake.Some");
        _ = loggerFactory.CreateLogger("Fake.Category.AtDebugLevel.Some");

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "effectiveLevel": "ERROR"
                },
                "Fake": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category.AtDebugLevel": {
                  "effectiveLevel": "DEBUG"
                },
                "Fake.Category.AtDebugLevel.Some": {
                  "effectiveLevel": "DEBUG"
                },
                "Fake.Some": {
                  "effectiveLevel": "WARN"
                }
              },
              "groups": {}
            }
            """);

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Fake.Some": "Error",
              "Fake.Category": "Warning"
            }
          }
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/loggers"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "levels": [
                "OFF",
                "FATAL",
                "ERROR",
                "WARN",
                "INFO",
                "DEBUG",
                "TRACE"
              ],
              "loggers": {
                "Default": {
                  "effectiveLevel": "INFO"
                },
                "Fake": {
                  "effectiveLevel": "INFO"
                },
                "Fake.Category": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category.AtDebugLevel": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Category.AtDebugLevel.Some": {
                  "effectiveLevel": "WARN"
                },
                "Fake.Some": {
                  "effectiveLevel": "ERROR"
                }
              },
              "groups": {}
            }
            """);
    }

    private static void EnsureLoggingConfigurationIsBound(ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        // This normally happens automatically, but is needed because we use empty host builders in tests.
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
    }
}
