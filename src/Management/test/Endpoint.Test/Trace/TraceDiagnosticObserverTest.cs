// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public sealed class TraceDiagnosticObserverTest : BaseTest
{
    [Fact]
    public void GetSessionId_NoSession_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = observer.GetSessionId(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetSessionId_WithSession_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        var session = new TestSession();

        ISessionFeature sessionFeature = new SessionFeature
        {
            Session = session
        };

        context.Features.Set(sessionFeature);

        string result = observer.GetSessionId(context);
        Assert.Equal("TestSessionId", result);
    }

    [Fact]
    public void GetUserPrincipal_NotAuthenticated_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = observer.GetUserPrincipal(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetUserPrincipal_Authenticated_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        context.User = new ClaimsPrincipal(new MyIdentity());
        string result = observer.GetUserPrincipal(context);
        Assert.Equal("MyTestName", result);
    }

    [Fact]
    public void GetRemoteAddress_NoConnection_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = observer.GetRemoteAddress(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetPathInfo_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        string result = observer.GetPathInfo(context.Request);
        Assert.Equal("/myPath", result);
    }

    [Fact]
    public void GetRequestUri_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = observer.GetRequestUri(context.Request);
        Assert.Equal("http://localhost:1111/myPath", result);
    }

    [Fact]
    public void GetRequestParameters_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        Dictionary<string, string[]> result = observer.GetRequestParameters(context.Request);
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("foo"));
        Assert.True(result.ContainsKey("bar"));
        string[] fooValue = result["foo"];
        Assert.Single(fooValue);
        Assert.Equal("bar", fooValue[0]);
        string[] barValue = result["bar"];
        Assert.Single(barValue);
        Assert.Equal("foo", barValue[0]);
    }

    [Fact]
    public void GetTimeTaken_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        TimeSpan time = TimeSpan.FromTicks(10_000_000);
        string result = observer.GetTimeTaken(time);
        string expected = time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetHeaders_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        Dictionary<string, object> result = observer.GetHeaders(100, context.Request.Headers);
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("header1"));
        Assert.True(result.ContainsKey("header2"));
        Assert.True(result.ContainsKey("status"));
        string header1Value = result["header1"] as string;
        Assert.Equal("header1Value", header1Value);
        string header2Value = result["header2"] as string;
        Assert.Equal("header2Value", header2Value);
        string statusValue = result["status"] as string;
        Assert.Equal("100", statusValue);
    }

    [Fact]
    public void GetProperty_NoProperties_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        HttpContext context = observer.GetHttpContextPropertyValue(new
        {
            foo = "bar"
        });

        Assert.Null(context);
    }

    [Fact]
    public void GetProperty_WithProperties_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext expectedContext = CreateRequest();

        HttpContext context = observer.GetHttpContextPropertyValue(new
        {
            HttpContext = expectedContext
        });

        Assert.True(ReferenceEquals(expectedContext, context));
    }

    [Fact]
    public void MakeTrace_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        TimeSpan duration = TimeSpan.FromTicks(20_000_000 - 10_000_000);
        TraceResult result = observer.MakeTrace(context, duration);
        Assert.NotNull(result);
        Assert.NotNull(result.Info);
        Assert.NotEqual(0, result.TimeStamp);
        Assert.True(result.Info.ContainsKey("method"));
        Assert.True(result.Info.ContainsKey("path"));
        Assert.True(result.Info.ContainsKey("headers"));
        Assert.True(result.Info.ContainsKey("timeTaken"));
        Assert.Equal("GET", result.Info["method"]);
        Assert.Equal("/myPath", result.Info["path"]);
        var headers = result.Info["headers"] as Dictionary<string, object>;
        Assert.NotNull(headers);
        Assert.True(headers.ContainsKey("request"));
        Assert.True(headers.ContainsKey("response"));
        string timeTaken = result.Info["timeTaken"] as string;
        Assert.NotNull(timeTaken);
        string expected = duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, timeTaken);
    }

    [Fact]
    public void ProcessEvent_IgnoresUnprocessableEvents()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        // No current activity, event ignored
        observer.ProcessEvent("foobar", null);

        var current = new Activity("barfoo");
        current.Start();

        // Activity current, but no value provided, event ignored
        observer.ProcessEvent("foobar", null);

        // Activity current, value provided, event not stop event, event is ignored
        observer.ProcessEvent("foobar", new object());

        // Activity current, event is stop event, no context in event value, event it ignored
        observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new object());

        Assert.Empty(observer.Queue);
        current.Stop();
    }

    [Fact]
    public void Subscribe_Listener_StopActivity_AddsToQueue()
    {
        using var listener = new DiagnosticListener("Microsoft.AspNetCore");
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        using var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        observer.Subscribe(listener);

        HttpContext context = CreateRequest();
        const string activityName = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
        var current = new Activity(activityName);

        listener.StartActivity(current, new
        {
            HttpContext = context
        });

        Thread.Sleep(1000);

        listener.StopActivity(current, new
        {
            HttpContext = context
        });

        Assert.Single(observer.Queue);
        Assert.True(observer.Queue.TryPeek(out TraceResult result));
        Assert.NotNull(result.Info);
        Assert.NotEqual(0, result.TimeStamp);
        Assert.True(result.Info.ContainsKey("method"));
        Assert.True(result.Info.ContainsKey("path"));
        Assert.True(result.Info.ContainsKey("headers"));
        Assert.True(result.Info.ContainsKey("timeTaken"));
        Assert.Equal("GET", result.Info["method"]);
        Assert.Equal("/myPath", result.Info["path"]);
        var headers = result.Info["headers"] as Dictionary<string, object>;
        Assert.NotNull(headers);
        Assert.True(headers.ContainsKey("request"));
        Assert.True(headers.ContainsKey("response"));
        short timeTaken = short.Parse((string)result.Info["timeTaken"], CultureInfo.InvariantCulture);
        Assert.InRange(timeTaken, 1000, 1300);
    }

    [Fact]
    public void ProcessEvent_AddsToQueue()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        HttpContext context = CreateRequest();

        observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
        {
            HttpContext = context
        });

        Assert.Single(observer.Queue);

        Assert.True(observer.Queue.TryPeek(out TraceResult result));
        Assert.NotNull(result.Info);
        Assert.NotEqual(0, result.TimeStamp);
        Assert.True(result.Info.ContainsKey("method"));
        Assert.True(result.Info.ContainsKey("path"));
        Assert.True(result.Info.ContainsKey("headers"));
        Assert.True(result.Info.ContainsKey("timeTaken"));
        Assert.Equal("GET", result.Info["method"]);
        Assert.Equal("/myPath", result.Info["path"]);
        var headers = result.Info["headers"] as Dictionary<string, object>;
        Assert.NotNull(headers);
        Assert.True(headers.ContainsKey("request"));
        Assert.True(headers.ContainsKey("response"));
        string timeTaken = result.Info["timeTaken"] as string;
        Assert.NotNull(timeTaken);
        Assert.Equal("0", timeTaken); // 0 because activity not stopped

        current.Stop();
    }

    [Fact]
    public void ProcessEvent_HonorsCapacity()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int index = 0; index < 200; index++)
        {
            HttpContext context = CreateRequest();

            observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
            {
                HttpContext = context
            });
        }

        Assert.Equal(option.CurrentValue.Capacity, observer.Queue.Count);
    }

    [Fact]
    public void GetTraces_ReturnsTraces()
    {
        using var listener = new DiagnosticListener("test");
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var observer = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int index = 0; index < 200; index++)
        {
            HttpContext context = CreateRequest();

            observer.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
            {
                HttpContext = context
            });
        }

        Assert.Equal(option.CurrentValue.Capacity, observer.Queue.Count);
        var result = (HttpTraceResultV1)observer.GetTraces();
        Assert.Equal(option.CurrentValue.Capacity, result.Traces.Count);
        Assert.Equal(option.CurrentValue.Capacity, observer.Queue.Count);
    }

    private HttpContext CreateRequest()
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = "GET";
        context.Request.Path = "/myPath";
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost:1111");
        context.Request.QueryString = new QueryString("?foo=bar&bar=foo");
        context.Request.Headers.Add("Header1", new StringValues("header1Value"));
        context.Request.Headers.Add("Header2", new StringValues("header2Value"));
        return context;
    }
}
