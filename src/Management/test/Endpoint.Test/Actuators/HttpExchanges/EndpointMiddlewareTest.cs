// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:httpExchanges:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "httpexchanges"
    };

    [Fact]
    public async Task HttpExchangesActuator_DoesNotCaptureAuthInUri()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        var requestUri = new Uri("http://username:password@localhost/actuator/httpexchanges");
        _ = await httpClient.GetAsync(requestUri);
        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("username").And.NotContain("password");
        json.Should().Contain("http://localhost:80/actuator/httpexchanges");
    }

    [Fact]
    public async Task HttpExchangesActuator_ReturnsExpectedData()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var httpExchanges = await response.Content.ReadFromJsonAsync<HttpExchangesResult>();
        httpExchanges!.Exchanges.Should().BeEmpty();

        response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("username").And.NotContain("password");
        json.Should().Contain("http://localhost:80/actuator/httpexchanges");
        json.Should().Contain($"\"timestamp\":\"{DateTime.UtcNow.Date:yyyy-MM-dd}");
    }

    [Fact]
    public async Task HttpExchangesActuator_IncludeAll_RendersResponse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "httpexchanges",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:httpExchanges:enabled"] = "true",
            ["management:endpoints:httpExchanges:IncludeRequestHeaders"] = "true",
            ["management:endpoints:httpExchanges:IncludeResponseHeaders"] = "true",
            ["management:endpoints:httpExchanges:IncludePathInfo"] = "true",
            ["management:endpoints:httpExchanges:IncludeQueryString"] = "true",
            ["management:endpoints:httpExchanges:IncludeUserPrincipal"] = "true",
            ["management:endpoints:httpExchanges:IncludeRemoteAddress"] = "true",
            ["management:endpoints:httpExchanges:IncludeSessionId"] = "true",
            ["management:endpoints:httpExchanges:IncludeTimeTaken"] = "true"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();

        var observer = (HttpExchangesDiagnosticObserver)host.Services.GetRequiredService<IHttpExchangesRepository>();
        HttpExchange exchange = CreateTestHttpExchange();
        observer.Queue.Enqueue(exchange);

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timeTaken": "PT0.027S",
                  "timestamp": "2022-01-31T19:43:25Z",
                  "principal": {
                    "name": "user1"
                  },
                  "session": {
                    "id": "C1823B32-3E02-420C-B6B4-83822BEA42B7"
                  },
                  "request": {
                    "method": "GET",
                    "uri": "https://www.example.com:9999/path?query",
                    "headers": {
                      "Accept": [
                        "*/*"
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
                        "999"
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
    public async Task HttpExchangesActuator_IncludeNone_RendersResponse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "httpexchanges",
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:httpExchanges:enabled"] = "true",
            ["management:endpoints:httpExchanges:IncludeRequestHeaders"] = "false",
            ["management:endpoints:httpExchanges:IncludeResponseHeaders"] = "false",
            ["management:endpoints:httpExchanges:IncludePathInfo"] = "false",
            ["management:endpoints:httpExchanges:IncludeQueryString"] = "false",
            ["management:endpoints:httpExchanges:IncludeUserPrincipal"] = "false",
            ["management:endpoints:httpExchanges:IncludeRemoteAddress"] = "false",
            ["management:endpoints:httpExchanges:IncludeSessionId"] = "false",
            ["management:endpoints:httpExchanges:IncludeTimeTaken"] = "false"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();

        host.UseRouting();
        await host.StartAsync();

        var observer = (HttpExchangesDiagnosticObserver)host.Services.GetRequiredService<IHttpExchangesRepository>();
        HttpExchange exchange = CreateTestHttpExchange();
        observer.Queue.Enqueue(exchange);

        using HttpClient httpClient = host.GetTestClient();
        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/httpexchanges"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string json = await response.Content.ReadAsStringAsync();

        json.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timeTaken": "PT0.027S",
                  "timestamp": "2022-01-31T19:43:25Z",
                  "request": {
                    "method": "GET",
                    "uri": "https://www.example.com:9999/"
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
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<HttpExchangesEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        endpointOptions.RequiresExactMatch().Should().BeTrue();
        endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path).Should().Be("/actuator/httpexchanges");

        endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath).Should()
            .Be("/cloudfoundryapplication/httpexchanges");

        endpointOptions.AllowedVerbs.Should().Contain(verb => verb == "Get");
    }

    private static HttpExchange CreateTestHttpExchange()
    {
        var requestHeader = new Dictionary<string, StringValues>
        {
            ["Accept"] = "*/*",
            ["X-Redacted-Request-Header"] = "Redact-Me"
        };

        var request = new HttpExchangeRequest("GET", new Uri("https://www.example.com:9999/path?query"), requestHeader, "192.168.0.1");

        var responseHeaders = new Dictionary<string, StringValues>
        {
            ["Content-Length"] = "999",
            ["X-Redacted-Response-Header"] = "Redact-Me"
        };

        var response = new HttpExchangeResponse((int)HttpStatusCode.OK, responseHeaders);

        DateTime timestamp = 31.January(2022).At(19, 43, 25).AsUtc();
        var principal = new HttpExchangePrincipal("user1");
        var session = new HttpExchangeSession("C1823B32-3E02-420C-B6B4-83822BEA42B7");

        return new HttpExchange(request, response, timestamp, principal, session, TimeSpan.FromMilliseconds(27));
    }
}
