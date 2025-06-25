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
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicSerilog;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class LoggersActuatorSerilogTest
{
    private static readonly MediaTypeHeaderValue RequestContentType = MediaTypeHeaderValue.Parse("application/vnd.spring-boot.actuator.v3+json");

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "loggers"
    };

    [Fact]
    public async Task Can_get_minimum_levels_with_serilog()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Serilog:MinimumLevel:Default"] = "Error",
            ["Serilog:MinimumLevel:Override:Fake"] = "Warning",
            ["Serilog:MinimumLevel:Override:Fake.Category.AtDebugLevel"] = "Debug"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Logging.AddDynamicSerilog();
        builder.Services.AddLoggersActuator();
        await using WebApplication host = builder.Build();

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
    public async Task Can_change_minimum_levels_with_serilog()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Serilog:MinimumLevel:Default"] = "Error",
            ["Serilog:MinimumLevel:Override:Fake"] = "Warning",
            ["Serilog:MinimumLevel:Override:Fake.Category.AtDebugLevel"] = "Debug"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Logging.AddDynamicSerilog();
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
    public async Task Can_change_serilog_configuration_at_runtime()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Serilog": {
            "MinimumLevel": {
              "Default": "Error",
              "Override": {
                "Fake": "Warning",
                "Fake.Category.AtDebugLevel": "Debug"
              }
            }
          }
        }
        """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, MemoryFileProvider.DefaultAppSettingsFileName, false, true);
        builder.Services.AddSingleton<ILoggerFactory, OnlyTrackFakeCategoryLoggerFactory>();
        builder.Logging.AddDynamicSerilog();
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

        fileProvider.ReplaceFile(MemoryFileProvider.DefaultAppSettingsFileName, """
        {
          "Serilog": {
            "MinimumLevel": {
              "Default": "Information",
              "Override": {
                "Fake.Some": "Error",
                "Fake.Category": "Warning"
              }
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
}
