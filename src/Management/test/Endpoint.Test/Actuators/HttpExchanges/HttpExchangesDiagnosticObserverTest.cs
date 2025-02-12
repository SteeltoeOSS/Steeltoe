// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class HttpExchangesDiagnosticObserverTest : BaseTest
{
    [Fact]
    public void ProcessEvent_IgnoresUnprocessableEvents()
    {
        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>();

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);

        // No current activity, event ignored
        observer.ProcessEvent("foobar", null);

        var current = new Activity("bar-foo");
        current.Start();

        // Activity current, but no value provided, event ignored
        observer.ProcessEvent("foobar", null);

        // Activity current, value provided, event not stop event, event is ignored
        observer.ProcessEvent("foobar", new object());

        // Activity current, event is stop event, no context in event value, event it ignored
        observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new object());

        observer.Queue.Should().BeEmpty();
        current.Stop();
    }

    [Fact]
    public async Task Subscribe_Listener_StopActivity_AddsToQueue()
    {
        using var listener = new DiagnosticListener("Microsoft.AspNetCore");

        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>();

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);
        observer.Subscribe(listener);

        HttpContext context = CreateRequest();
        const string activityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        var current = new Activity(activityName);

        listener.StartActivity(current, new
        {
            HttpContext = context
        });

        await Task.Delay(1000);

        listener.StopActivity(current, context);

        HttpExchange result = PerformCommonAssertions(observer);
        result.TimeTaken.Should().BeGreaterThan(TimeSpan.FromMilliseconds(900));
        result.TimeTaken.Should().BeLessThan(TimeSpan.FromMilliseconds(1300));
    }

    [Fact]
    public void ProcessEvent_AddsToQueue()
    {
        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>();

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);

        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        HttpContext context = CreateRequest();

        observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", context);

        HttpExchange result = PerformCommonAssertions(observer);
        result.SerializedTimeTaken.Should().Be("PT0S"); // 0 because activity not stopped

        current.Stop();
    }

    [Fact]
    public void ProcessEvent_HonorsCapacity()
    {
        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>();

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int index = 0; index < 200; index++)
        {
            HttpContext context = CreateRequest();

            observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", context);
        }

        observer.Queue.Should().HaveCount(optionsMonitor.CurrentValue.Capacity);
    }

    [Fact]
    public void GetHttpExchanges_ReturnsHttpExchanges()
    {
        using var listener = new DiagnosticListener("test");

        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>();

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int index = 0; index < 200; index++)
        {
            HttpContext context = CreateRequest();
            context.Request.QueryString = new QueryString("?requestNumber=" + index);
            observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", context);
        }

        observer.Queue.Should().HaveCount(optionsMonitor.CurrentValue.Capacity);
        HttpExchangesResult result = observer.GetHttpExchanges();
        result.Exchanges.Should().HaveCount(optionsMonitor.CurrentValue.Capacity);
        observer.Queue.Should().HaveCount(optionsMonitor.CurrentValue.Capacity);
        result.Exchanges[0].Request.Uri.Query.Should().Be("?requestNumber=199");

        optionsMonitor.CurrentValue.Reverse = false;
        result = observer.GetHttpExchanges();
        result.Exchanges[0].Request.Uri.Query.Should().EndWith("requestNumber=100");
    }

    [Fact]
    public void GetHttpExchanges_FiltersDuringReturn()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:httpExchanges:IncludeUserPrincipal"] = "true",
            ["management:endpoints:httpExchanges:IncludeRemoteAddress"] = "true",
            ["management:endpoints:httpExchanges:IncludeSessionId"] = "true",
            ["management:endpoints:httpExchanges:RequestHeaders:0"] = "header2",
            ["management:endpoints:httpExchanges:ResponseHeaders:0"] = "HeaderB"
        };

        using var listener = new DiagnosticListener("test");

        IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor =
            GetOptionsMonitorFromSettings<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions>(appSettings);

        using var observer = new HttpExchangesDiagnosticObserver(optionsMonitor, TimeProvider.System, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        HttpContext context = CreateRequest();
        context.User = new ClaimsPrincipal(new MyIdentity());
        ISessionFeature sessionFeature = new SessionFeature(new TestSession());

        context.Features.Set(sessionFeature);

        observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", context);

        observer.Queue.Should().ContainSingle();
        HttpExchange queuedExchange = observer.Queue.First();

        queuedExchange.Should().NotBeNull();
        queuedExchange.Request.Headers[HeaderNames.Accept][0].Should().Be("testContent");
        queuedExchange.Request.Headers[HeaderNames.Authorization][0].Should().Be("bearer TestToken");
        queuedExchange.Request.Headers[HeaderNames.Host][0].Should().Be("localhost:1111");
        queuedExchange.Request.Headers[HeaderNames.UserAgent][0].Should().Be("TestHost");
        queuedExchange.Request.Headers["Header1"][0].Should().Be("header1Value");
        queuedExchange.Request.Headers["Header2"][0].Should().Be("header2Value");
        queuedExchange.Response.Headers["HeaderA"][0].Should().Be("headerAValue");
        queuedExchange.Response.Headers["HeaderB"][0].Should().Be("headerBValue");
        queuedExchange.Response.Headers[HeaderNames.SetCookie][0].Should().Be("some-value-that-should-be-redacted");
        queuedExchange.Principal.Should().NotBeNull();
        queuedExchange.Principal!.Name.Should().Be("MyTestName");
        queuedExchange.Request.RemoteAddress.Should().Be("127.0.0.1");
        queuedExchange.Session.Should().NotBeNull();
        queuedExchange.Session!.Id.Should().Be("TestSessionId");

        HttpExchange filteredHeaders = observer.GetHttpExchanges().Exchanges[0];
        filteredHeaders.Should().NotBeNull();
        filteredHeaders.Request.Uri.PathAndQuery.Should().Be("/myPath?foo=bar&bar=foo");
        filteredHeaders.Request.Headers[HeaderNames.Accept][0].Should().Be("testContent");
        filteredHeaders.Request.Headers[HeaderNames.Authorization][0].Should().Be(HttpExchangesDiagnosticObserver.Redacted);
        filteredHeaders.Request.Headers[HeaderNames.Host][0].Should().Be("localhost:1111");
        filteredHeaders.Request.Headers[HeaderNames.UserAgent][0].Should().Be("TestHost");
        filteredHeaders.Request.Headers["Header1"][0].Should().Be(HttpExchangesDiagnosticObserver.Redacted);
        filteredHeaders.Request.Headers["Header2"][0].Should().Be("header2Value");
        filteredHeaders.Response.Headers["HeaderA"][0].Should().Be(HttpExchangesDiagnosticObserver.Redacted);
        filteredHeaders.Response.Headers["HeaderB"][0].Should().Be("headerBValue");
        filteredHeaders.Response.Headers[HeaderNames.SetCookie][0].Should().Be(HttpExchangesDiagnosticObserver.Redacted);
        filteredHeaders.Principal.Should().NotBeNull();
        filteredHeaders.Principal!.Name.Should().Be("MyTestName");
        filteredHeaders.Request.RemoteAddress.Should().Be("127.0.0.1");
        filteredHeaders.Session.Should().NotBeNull();
        filteredHeaders.Session!.Id.Should().Be("TestSessionId");

        optionsMonitor.CurrentValue.RequestHeaders.Add(HeaderNames.Authorization);
        optionsMonitor.CurrentValue.RequestHeaders.Add("header1");
        optionsMonitor.CurrentValue.ResponseHeaders.Add("headera"); // tests case-insensitivity
        optionsMonitor.CurrentValue.ResponseHeaders.Add(HeaderNames.SetCookie);

        HttpExchange unfilteredHeaders = observer.GetHttpExchanges().Exchanges[0];
        unfilteredHeaders.Should().NotBeNull();
        unfilteredHeaders.Request.Headers[HeaderNames.Accept][0].Should().Be("testContent");
        unfilteredHeaders.Request.Headers[HeaderNames.Authorization][0].Should().Be("bearer TestToken");
        unfilteredHeaders.Request.Headers[HeaderNames.Host][0].Should().Be("localhost:1111");
        unfilteredHeaders.Request.Headers[HeaderNames.UserAgent][0].Should().Be("TestHost");
        unfilteredHeaders.Request.Headers["Header1"][0].Should().Be("header1Value");
        unfilteredHeaders.Request.Headers["Header2"][0].Should().Be("header2Value");
        unfilteredHeaders.Response.Headers["HeaderA"][0].Should().Be("headerAValue");
        unfilteredHeaders.Response.Headers["HeaderB"][0].Should().Be("headerBValue");
        unfilteredHeaders.Response.Headers[HeaderNames.SetCookie][0].Should().Be("some-value-that-should-be-redacted");

        optionsMonitor.CurrentValue.IncludePathInfo = false;
        optionsMonitor.CurrentValue.IncludeQueryString = false;
        optionsMonitor.CurrentValue.IncludeUserPrincipal = false;
        optionsMonitor.CurrentValue.IncludeRemoteAddress = false;
        optionsMonitor.CurrentValue.IncludeRequestHeaders = false;
        optionsMonitor.CurrentValue.IncludeResponseHeaders = false;
        optionsMonitor.CurrentValue.IncludeSessionId = false;
        optionsMonitor.CurrentValue.IncludeTimeTaken = false;

        HttpExchange excludedDetails = observer.GetHttpExchanges().Exchanges[0];
        excludedDetails.Should().NotBeNull();
        excludedDetails.Request.Uri.PathAndQuery.Should().Be("/");
        excludedDetails.Principal.Should().BeNull();
        excludedDetails.Request.RemoteAddress.Should().BeNull();
        excludedDetails.Request.Headers.Should().BeEmpty();
        excludedDetails.Response.Headers.Should().BeEmpty();
        excludedDetails.Session.Should().BeNull();
    }

    private static HttpContext CreateRequest()
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Features.Set<ISessionFeature>(new DefaultSessionFeature());
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Method = "GET";
        context.Request.Path = "/myPath";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 1111);
        context.Request.QueryString = new QueryString("?foo=bar&bar=foo");
        context.Request.Headers.Append(HeaderNames.Accept, new StringValues("testContent"));
        context.Request.Headers.Append(HeaderNames.Authorization, new StringValues("bearer TestToken"));
        context.Request.Headers.Append(HeaderNames.UserAgent, new StringValues("TestHost"));
        context.Request.Headers.Append("Header1", new StringValues("header1Value"));
        context.Request.Headers.Append("Header2", new StringValues("header2Value"));
        context.Response.Body = new MemoryStream();
        context.Response.Headers.Append("HeaderA", new StringValues("headerAValue"));
        context.Response.Headers.Append("HeaderB", new StringValues("headerBValue"));
        context.Response.Headers.Append(HeaderNames.SetCookie, "some-value-that-should-be-redacted");
        return context;
    }

    private static HttpExchange PerformCommonAssertions(HttpExchangesDiagnosticObserver observer)
    {
        observer.Queue.Should().ContainSingle();
        observer.Queue.TryPeek(out HttpExchange? result).Should().BeTrue();
        result.Should().NotBeNull();
        result!.Request.Method.Should().Be("GET");
        result.Request.Uri.Should().Be("http://localhost:1111/myPath?foo=bar&bar=foo");
        result.Request.Headers.Should().ContainKeys(HeaderNames.Accept, HeaderNames.Authorization, HeaderNames.UserAgent, "Header1", "Header2");

        return result;
    }
}
