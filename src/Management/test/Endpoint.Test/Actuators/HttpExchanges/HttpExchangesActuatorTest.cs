// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class HttpExchangesActuatorTest
{
    private static readonly Dictionary<string, StringValues> EmptyHeaderDictionary = [];

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "httpexchanges"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHttpExchangesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Func<HttpExchangesEndpointMiddleware> action = serviceProvider.GetRequiredService<HttpExchangesEndpointMiddleware>;
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddHttpExchangesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        HttpExchangesEndpointOptions options = serviceProvider.GetRequiredService<IOptions<HttpExchangesEndpointOptions>>().Value;

        options.Capacity.Should().Be(100);
        options.IncludeRequestHeaders.Should().BeTrue();
        options.IncludeResponseHeaders.Should().BeTrue();
        options.IncludePathInfo.Should().BeTrue();
        options.IncludeQueryString.Should().BeTrue();
        options.IncludeUserPrincipal.Should().BeFalse();
        options.IncludeRemoteAddress.Should().BeFalse();
        options.IncludeSessionId.Should().BeFalse();
        options.IncludeTimeTaken.Should().BeTrue();

        options.RequestHeaders.Should().BeEquivalentTo("Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Allow", "Cache-Control", "Connection",
            "Content-Encoding", "Content-Length", "Content-Type", "Date", "DNT", "Expect", "Host", "Max-Forwards", "Range", "Sec-WebSocket-Extensions",
            "Sec-WebSocket-Version", "TE", "Trailer", "Transfer-Encoding", "Upgrade", "User-Agent", "Warning", "X-Requested-With", "X-UA-Compatible");

        options.ResponseHeaders.Should().BeEquivalentTo("Accept-Ranges", "Age", "Allow", "Alt-Svc", "Connection", "Content-Disposition", "Content-Language",
            "Content-Length", "Content-Location", "Content-Range", "Content-Type", "Date", "Expires", "Last-Modified", "Location", "Server",
            "Transfer-Encoding", "Upgrade", "X-Powered-By");

        options.Reverse.Should().BeTrue();
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("httpexchanges");
        options.Path.Should().Be("httpexchanges");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/httpexchanges");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:HttpExchanges:Capacity"] = "250",
            ["Management:Endpoints:HttpExchanges:IncludeRequestHeaders"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeResponseHeaders"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludePathInfo"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeQueryString"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeUserPrincipal"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeRemoteAddress"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeSessionId"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeTimeTaken"] = "false",
            ["Management:Endpoints:HttpExchanges:RequestHeaders:0"] = "Authorization",
            ["Management:Endpoints:HttpExchanges:ResponseHeaders:0"] = "Set-Cookie",
            ["Management:Endpoints:HttpExchanges:Reverse"] = "false",
            ["Management:Endpoints:HttpExchanges:Enabled"] = "true",
            ["Management:Endpoints:HttpExchanges:Id"] = "test-actuator-id",
            ["Management:Endpoints:HttpExchanges:Path"] = "test-actuator-path",
            ["Management:Endpoints:HttpExchanges:RequiredPermissions"] = "full",
            ["Management:Endpoints:HttpExchanges:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddHttpExchangesActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        HttpExchangesEndpointOptions options = serviceProvider.GetRequiredService<IOptions<HttpExchangesEndpointOptions>>().Value;

        options.Capacity.Should().Be(250);
        options.IncludeRequestHeaders.Should().BeFalse();
        options.IncludeResponseHeaders.Should().BeFalse();
        options.IncludePathInfo.Should().BeFalse();
        options.IncludeQueryString.Should().BeFalse();
        options.IncludeUserPrincipal.Should().BeTrue();
        options.IncludeRemoteAddress.Should().BeTrue();
        options.IncludeSessionId.Should().BeTrue();
        options.IncludeTimeTaken.Should().BeFalse();
        options.RequestHeaders.Should().Contain("Authorization", "Accept");
        options.ResponseHeaders.Should().Contain("Set-Cookie", "Accept-Ranges");
        options.Reverse.Should().BeFalse();
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data_without_filters(HostBuilderType hostBuilderType)
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:HttpExchanges:IncludeUserPrincipal"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeRemoteAddress"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeSessionId"] = "true"
        };

        HttpExchange httpExchange = CreateTestHttpExchange();

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHttpExchangeRecorder>(new FakeHttpExchangeRecorder([httpExchange]));
                services.AddHttpExchangesActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timeTaken": "PT0.364S",
                  "timestamp": "2025-01-01T21:18:43Z",
                  "principal": {
                    "name": "test-user"
                  },
                  "session": {
                    "id": "DA731132-F24C-4D4E-AAF3-8BE554AE1FAA"
                  },
                  "request": {
                    "method": "GET",
                    "uri": "http://api.test.com:8080/path/to/data?filter=A",
                    "headers": {
                      "Accept": [
                        "application/json"
                      ],
                      "X-Redacted-Request-Header": [
                        "******"
                      ]
                    },
                    "remoteAddress": "192.168.0.1"
                  },
                  "response": {
                    "status": 200,
                    "headers": {
                      "Content-Length": [
                        "8192"
                      ],
                      "X-Redacted-Response-Header": [
                        "******"
                      ]
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Endpoint_returns_expected_data_with_filters()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:HttpExchanges:IncludeRequestHeaders"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeResponseHeaders"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludePathInfo"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeQueryString"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeUserPrincipal"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeRemoteAddress"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeSessionId"] = "false",
            ["Management:Endpoints:HttpExchanges:IncludeTimeTaken"] = "false"
        };

        HttpExchange httpExchange = CreateTestHttpExchange();

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IHttpExchangeRecorder>(new FakeHttpExchangeRecorder([httpExchange]));
        builder.Services.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timestamp": "2025-01-01T21:18:43Z",
                  "request": {
                    "method": "GET",
                    "uri": "http://api.test.com:8080/"
                  },
                  "response": {
                    "status": 200
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Configured_header_names_are_case_insensitive()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:HttpExchanges:RequestHeaders:0"] = "X-WHITELISTED-REQUEST-HEADER",
            ["Management:Endpoints:HttpExchanges:ResponseHeaders:0"] = "X-WHITELISTED-RESPONSE-HEADER"
        };

        HttpExchange httpExchange = new(new HttpExchangeRequest("POST", new Uri("http://localhost"), new Dictionary<string, StringValues>
        {
            ["X-Whitelisted-Request-Header"] = "visible-request-header-value"
        }, null), new HttpExchangeResponse((int)HttpStatusCode.OK, new Dictionary<string, StringValues>
        {
            ["X-Whitelisted-Response-Header"] = "visible-response-header-value"
        }), 1.January(2025).At(21, 18, 43).AsUtc(), null, null, 43.Milliseconds());

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IHttpExchangeRecorder>(new FakeHttpExchangeRecorder([httpExchange]));
        builder.Services.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timeTaken": "PT0.043S",
                  "timestamp": "2025-01-01T21:18:43Z",
                  "request": {
                    "method": "POST",
                    "uri": "http://localhost:80/",
                    "headers": {
                      "X-Whitelisted-Request-Header": [
                        "visible-request-header-value"
                      ]
                    }
                  },
                  "response": {
                    "status": 200,
                    "headers": {
                      "X-Whitelisted-Response-Header": [
                        "visible-response-header-value"
                      ]
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Respects_maximum_queue_capacity()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["Management:Endpoints:HttpExchanges:Capacity"] = "5"
        };

        DateTime currentTime = 19.September(2024);
        List<HttpExchange> httpExchanges = [];

        for (int index = 1; index <= 25; index++)
        {
            var requestUri = new Uri($"http://localhost/id/{index}");
            DateTime timestamp = currentTime.AddSeconds(index);

            var httpExchange = new HttpExchange(new HttpExchangeRequest("GET", requestUri, EmptyHeaderDictionary, null),
                new HttpExchangeResponse(200, EmptyHeaderDictionary), timestamp, null, null, null);

            httpExchanges.Add(httpExchange);
        }

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IHttpExchangeRecorder>(new FakeHttpExchangeRecorder(httpExchanges));
        builder.Services.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timestamp": "2024-09-19T00:00:25",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/25"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:24",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/24"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:23",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/23"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:22",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/22"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:21",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/21"
                  },
                  "response": {
                    "status": 200
                  }
                }
              ]
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
          "Management": {
            "Endpoints": {
              "HttpExchanges": {
                "IncludeQueryString": false
              }
            }
          }
        }
        """);

        DateTime currentTime = 19.September(2024);

        List<HttpExchange> httpExchanges =
        [
            new(new HttpExchangeRequest("GET", new Uri("http://localhost/id/1?q=test-query-string"), EmptyHeaderDictionary, null),
                new HttpExchangeResponse(200, EmptyHeaderDictionary), currentTime.AddSeconds(1), null, null, null),
            new(new HttpExchangeRequest("GET", new Uri("http://localhost/id/2?q=test-query-string"), EmptyHeaderDictionary, null),
                new HttpExchangeResponse(200, EmptyHeaderDictionary), currentTime.AddSeconds(2), null, null, null)
        ];

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Configuration.AddJsonFile(fileProvider, appSettingsJsonFileName, false, true);
        builder.Services.AddSingleton<IHttpExchangeRecorder>(new FakeHttpExchangeRecorder(httpExchanges));
        builder.Services.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response1 = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody1.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timestamp": "2024-09-19T00:00:02",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/2"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:01",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/1"
                  },
                  "response": {
                    "status": 200
                  }
                }
              ]
            }
            """);

        fileProvider.ReplaceFile(appSettingsJsonFileName, """
        {
          "Management": {
            "Endpoints": {
              "HttpExchanges": {
                "Reverse": false
              }
            }
          }
        }
        """);

        fileProvider.NotifyChanged();

        HttpResponseMessage response2 = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody2.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timestamp": "2024-09-19T00:00:01",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/1?q=test-query-string"
                  },
                  "response": {
                    "status": 200
                  }
                },
                {
                  "timestamp": "2024-09-19T00:00:02",
                  "request": {
                    "method": "GET",
                    "uri": "http://localhost:80/id/2?q=test-query-string"
                  },
                  "response": {
                    "status": 200
                  }
                }
              ]
            }
            """);
    }

    private static HttpExchange CreateTestHttpExchange()
    {
        var requestHeaders = new Dictionary<string, StringValues>
        {
            ["Accept"] = "application/json",
            ["X-Redacted-Request-Header"] = "Redact-Me"
        };

        var responseHeaders = new Dictionary<string, StringValues>
        {
            ["Content-Length"] = "8192",
            ["X-Redacted-Response-Header"] = "Redact-Me"
        };

        var request = new HttpExchangeRequest("GET", new Uri("http://api.test.com:8080/path/to/data?filter=A"), requestHeaders, "192.168.0.1");
        var response = new HttpExchangeResponse((int)HttpStatusCode.OK, responseHeaders);

        return new HttpExchange(request, response, 1.January(2025).At(21, 18, 43).AsUtc(), new HttpExchangePrincipal("test-user"),
            new HttpExchangeSession("DA731132-F24C-4D4E-AAF3-8BE554AE1FAA"), 364.Milliseconds());
    }
}
