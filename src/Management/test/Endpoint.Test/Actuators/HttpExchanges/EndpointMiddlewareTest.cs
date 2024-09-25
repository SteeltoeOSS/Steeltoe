// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
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
    public async Task HttpExchangesActuator_ReturnsExpectedData()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.AddHttpExchangesActuator();
        await using WebApplication host = builder.Build();
        host.UseRouting();
        host.MapGet("/hello", () => "Hello World!");
        await host.StartAsync();
        using var httpClient = new HttpClient();
        HttpResponseMessage helloResponse = await httpClient.GetAsync(new Uri("http://localhost:5000/hello?someQuery=value"));
        helloResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseMessage actuatorResponse = await httpClient.GetAsync(new Uri("http://localhost:5000/actuator/httpexchanges"));
        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var actuatorRootElement = await actuatorResponse.Content.ReadFromJsonAsync<JsonElement>();
        JsonElement[] exchangesArray = [.. actuatorRootElement.GetProperty("exchanges").EnumerateArray()];
        exchangesArray.Should().HaveCount(1);
        JsonElement testExchange = exchangesArray[0];
        string requestTimestamp = testExchange.GetProperty("timestamp").ToString();
        requestTimestamp.Should().StartWith($"{DateTime.UtcNow.Date:yyyy-MM-dd}T");
        requestTimestamp.Should().EndWith("Z");
        string requestTimeTaken = testExchange.GetProperty("timeTaken").ToString();
        var timeTaken = XmlConvert.ToTimeSpan(requestTimeTaken);
        timeTaken.Should().BeGreaterThan(TimeSpan.Zero).And.BeLessThan(TimeSpan.FromSeconds(1));
        testExchange.Invoking(jsonElement => jsonElement.GetProperty("principal")).Should().Throw<KeyNotFoundException>();
        testExchange.Invoking(jsonElement => jsonElement.GetProperty("session")).Should().Throw<KeyNotFoundException>();

        JsonElement request = testExchange.GetProperty("request");
        string requestMethod = request.GetProperty("method").ToString();
        requestMethod.Should().Be("GET");
        string requestUri = request.GetProperty("uri").ToString();
        requestUri.Should().Be("http://localhost:5000/hello?someQuery=value");
        JsonElement requestHeaders = request.GetProperty("headers");
        JsonElement[] hostHeader = [.. requestHeaders.GetProperty("Host").EnumerateArray()];
        hostHeader.Should().HaveCount(1);
        hostHeader[0].ToString().Should().Be("localhost:5000");
        request.Invoking(jsonElement => jsonElement.GetProperty("remoteAddress")).Should().Throw<KeyNotFoundException>();

        JsonElement response = testExchange.GetProperty("response");
        string responseStatus = response.GetProperty("status").ToString();
        responseStatus.Should().Be("200");
        JsonElement responseHeaders = response.GetProperty("headers");
        JsonElement[] contentTypeHeader = [.. responseHeaders.GetProperty("Content-Type").EnumerateArray()];
        contentTypeHeader.Should().HaveCount(1);
        contentTypeHeader[0].ToString().Should().Be("text/plain; charset=utf-8");
        JsonElement[] serverHeader = [.. responseHeaders.GetProperty("Server").EnumerateArray()];
        serverHeader.Should().HaveCount(1);
        serverHeader[0].ToString().Should().Be("Kestrel");
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
