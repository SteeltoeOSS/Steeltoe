// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

[Collection("TestsForMemoryDumpsMustRunSequentially")]
[Trait("Category", "MemoryDumps")]
public sealed class DiagnosticObserverHttpExchangeRecorderTest
{
    private const string RequestStartOperationName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
    private const string RequestStopOperationName = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    [Fact]
    public async Task Records_http_requests()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "httpexchanges",
            ["Management:Endpoints:HttpExchanges:IncludeRequestHeaders"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeResponseHeaders"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludePathInfo"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeQueryString"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeUserPrincipal"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeRemoteAddress"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeSessionId"] = "true",
            ["Management:Endpoints:HttpExchanges:IncludeTimeTaken"] = "false"
        };

        TimeProvider timeProvider = new FakeTimeProvider(31.October(2024).At(23, 43, 16, 789).AsUtc());

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault(false);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton(timeProvider);
        builder.Services.AddHttpExchangesActuator();
        builder.Services.PostConfigure<HttpExchangesEndpointOptions>(options => options.ResponseHeaders.Remove(HeaderNames.Date));
        await using WebApplication host = builder.Build();

        host.MapGet("/hello", () => "Hello World!");
        await host.StartAsync(TestContext.Current.CancellationToken);

        using var httpClient = new HttpClient();

        HttpResponseMessage helloResponse =
            await httpClient.GetAsync(new Uri("http://127.0.0.1:5000/hello?someQuery=value"), TestContext.Current.CancellationToken);

        helloResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _ = await httpClient.GetAsync(new Uri("http://127.0.0.1:5000/does/not/exist"), TestContext.Current.CancellationToken);

        await Task.Delay(250.Milliseconds(), TestContext.Current.CancellationToken);

        HttpResponseMessage actuatorResponse =
            await httpClient.GetAsync(new Uri("http://127.0.0.1:5000/actuator/httpexchanges"), TestContext.Current.CancellationToken);

        actuatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await actuatorResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "exchanges": [
                {
                  "timestamp": "2024-10-31T23:43:16.789Z",
                  "request": {
                    "method": "GET",
                    "uri": "http://127.0.0.1:5000/hello?someQuery=value",
                    "headers": {
                      "Host": [
                        "127.0.0.1:5000"
                      ]
                    },
                    "remoteAddress": "127.0.0.1"
                  },
                  "response": {
                    "status": 200,
                    "headers": {
                      "Content-Type": [
                        "text/plain; charset=utf-8"
                      ],
                      "Date": [
                        "******"
                      ],
                      "Server": [
                        "Kestrel"
                      ],
                      "Transfer-Encoding": [
                        "chunked"
                      ]
                    }
                  }
                },
                {
                  "timestamp": "2024-10-31T23:43:16.789Z",
                  "request": {
                    "method": "GET",
                    "uri": "http://127.0.0.1:5000/does/not/exist",
                    "headers": {
                      "Host": [
                        "127.0.0.1:5000"
                      ]
                    },
                    "remoteAddress": "127.0.0.1"
                  },
                  "response": {
                    "status": 404,
                    "headers": {
                      "Date": [
                        "******"
                      ],
                      "Server": [
                        "Kestrel"
                      ],
                      "Content-Length": [
                        "0"
                      ]
                    }
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Ignores_unprocessable_events()
    {
        List<HttpExchange> recorded = [];

        using var recorder = new DiagnosticObserverHttpExchangeRecorder(TimeProvider.System, NullLoggerFactory.Instance);
        recorder.HandleRecording(recorded.Add);

        // No current activity, event ignored
        recorder.ProcessEvent("foobar", null);

        using var activity = new Activity("bar-foo");
        activity.Start();

        // Activity current, but no value provided, event ignored
        recorder.ProcessEvent("foobar", null);
        recorded.Should().BeEmpty();

        // Activity current, value provided, event not stop event, event is ignored
        recorder.ProcessEvent("foobar", new object());
        recorded.Should().BeEmpty();

        // Activity current, event is stop event, no context in event value, event it ignored
        recorder.ProcessEvent(RequestStopOperationName, new object());
        recorded.Should().BeEmpty();
    }

    [Fact]
    public void Subscribe_to_listener_records_http_exchange()
    {
        using var listener = new DiagnosticListener("Microsoft.AspNetCore");
        using var recorder = new DiagnosticObserverHttpExchangeRecorder(TimeProvider.System, NullLoggerFactory.Instance);

        List<HttpExchange> recorded = [];
        recorder.HandleRecording(recorded.Add);
        recorder.Subscribe(listener);

        HttpContext context = CreateTestHttpContext();
        using var activity = new Activity(RequestStartOperationName);

        listener.StartActivity(activity, new
        {
            HttpContext = context
        });

        SpinWait.SpinUntil(() => false, 1.Seconds());

        listener.StopActivity(activity, context);

        HttpExchange httpExchange = recorded.Should().ContainSingle().Subject;
        httpExchange.TimeTaken.Should().BeGreaterThan(900.Milliseconds()).And.BeLessThan(1300.Milliseconds());
    }

    [Fact]
    public void Unstopped_activity_has_no_duration()
    {
        using var recorder = new DiagnosticObserverHttpExchangeRecorder(TimeProvider.System, NullLoggerFactory.Instance);

        List<HttpExchange> recorded = [];
        recorder.HandleRecording(recorded.Add);

        HttpContext context = CreateTestHttpContext();
        using var activity = new Activity(RequestStartOperationName);

        activity.Start();
        recorder.ProcessEvent(RequestStopOperationName, context);

        HttpExchange httpExchange = recorded.Should().ContainSingle().Subject;
        httpExchange.SerializedTimeTaken.Should().Be("PT0S");
    }

    private static HttpContext CreateTestHttpContext()
    {
        HttpContext context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Scheme = "http",
                Host = new HostString("localhost", 8080)
            }
        };

        return context;
    }
}
