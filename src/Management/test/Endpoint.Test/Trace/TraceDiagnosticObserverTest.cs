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

public class TraceDiagnosticObserverTest : BaseTest
{
    [Fact]
    public void Constructor_ThrowsOnNulls()
    {
        const IOptionsMonitor<TraceEndpointOptions> options = null;

        var ex2 = Assert.Throws<ArgumentNullException>(() => new TraceDiagnosticObserver(options, NullLoggerFactory.Instance));
        Assert.Contains(nameof(options), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSessionId_NoSession_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = obs.GetSessionId(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetSessionId_WithSession_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        var session = new TestSession();

        ISessionFeature sessionFeature = new SessionFeature
        {
            Session = session
        };

        context.Features.Set(sessionFeature);

        string result = obs.GetSessionId(context);
        Assert.Equal("TestSessionId", result);
    }

    [Fact]
    public void GetUserPrincipal_NotAuthenticated_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = obs.GetUserPrincipal(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetUserPrincipal_Authenticated_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        context.User = new ClaimsPrincipal(new MyIdentity());
        string result = obs.GetUserPrincipal(context);
        Assert.Equal("MyTestName", result);
    }

    [Fact]
    public void GetRemoteAddress_NoConnection_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = obs.GetRemoteAddress(context);
        Assert.Null(result);
    }

    [Fact]
    public void GetPathInfo_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        string result = obs.GetPathInfo(context.Request);
        Assert.Equal("/myPath", result);
    }

    [Fact]
    public void GetRequestUri_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        string result = obs.GetRequestUri(context.Request);
        Assert.Equal("http://localhost:1111/myPath", result);
    }

    [Fact]
    public void GetRequestParameters_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        Dictionary<string, string[]> result = obs.GetRequestParameters(context.Request);
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("foo"));
        Assert.True(result.ContainsKey("bar"));
        string[] fooVal = result["foo"];
        Assert.Single(fooVal);
        Assert.Equal("bar", fooVal[0]);
        string[] barVal = result["bar"];
        Assert.Single(barVal);
        Assert.Equal("foo", barVal[0]);
    }

    [Fact]
    public void GetTimeTaken_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        TimeSpan time = TimeSpan.FromTicks(10_000_000);
        string result = obs.GetTimeTaken(time);
        string expected = time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetHeaders_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();

        Dictionary<string, object> result = obs.GetHeaders(100, context.Request.Headers);
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("header1"));
        Assert.True(result.ContainsKey("header2"));
        Assert.True(result.ContainsKey("status"));
        string header1Val = result["header1"] as string;
        Assert.Equal("header1Value", header1Val);
        string header2Val = result["header2"] as string;
        Assert.Equal("header2Value", header2Val);
        string statusVal = result["status"] as string;
        Assert.Equal("100", statusVal);
    }

    [Fact]
    public void GetProperty_NoProperties_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        HttpContext context = obs.GetHttpContextPropertyValue(new
        {
            foo = "bar"
        });

        Assert.Null(context);
    }

    [Fact]
    public void GetProperty_WithProperties_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext expectedContext = CreateRequest();

        HttpContext context = obs.GetHttpContextPropertyValue(new
        {
            HttpContext = expectedContext
        });

        Assert.True(ReferenceEquals(expectedContext, context));
    }

    [Fact]
    public void MakeTrace_ReturnsExpected()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        HttpContext context = CreateRequest();
        TimeSpan duration = TimeSpan.FromTicks(20_000_000 - 10_000_000);
        TraceResult result = obs.MakeTrace(context, duration);
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

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        // No current activity, event ignored
        obs.ProcessEvent("foobar", null);

        var current = new Activity("barfoo");
        current.Start();

        // Activity current, but no value provided, event ignored
        obs.ProcessEvent("foobar", null);

        // Activity current, value provided, event not stop event, event is ignored
        obs.ProcessEvent("foobar", new object());

        // Activity current, event is stop event, no context in event value, event it ignored
        obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new object());

        Assert.Empty(obs.Queue);
        current.Stop();
    }

    [Fact]
    public void Subscribe_Listener_StopActivity_AddsToQueue()
    {
        var listener = new DiagnosticListener("Microsoft.AspNetCore");
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        obs.Subscribe(listener);

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

        Assert.Single(obs.Queue);
        Assert.True(obs.Queue.TryPeek(out TraceResult result));
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
        short timeTaken = short.Parse(result.Info["timeTaken"] as string, CultureInfo.InvariantCulture);
        Assert.InRange(timeTaken, 1000, 1300);

        obs.Dispose();
        listener.Dispose();
    }

    [Fact]
    public void ProcessEvent_AddsToQueue()
    {
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);

        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        HttpContext context = CreateRequest();

        obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
        {
            HttpContext = context
        });

        Assert.Single(obs.Queue);

        Assert.True(obs.Queue.TryPeek(out TraceResult result));
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

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int i = 0; i < 200; i++)
        {
            HttpContext context = CreateRequest();

            obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
            {
                HttpContext = context
            });
        }

        Assert.Equal(option.CurrentValue.Capacity, obs.Queue.Count);
    }

    [Fact]
    public void GetTraces_ReturnsTraces()
    {
        var listener = new DiagnosticListener("test");
        IOptionsMonitor<TraceEndpointOptions> option = GetOptionsMonitorFromSettings<TraceEndpointOptions, ConfigureTraceEndpointOptions>();

        var obs = new TraceDiagnosticObserver(option, NullLoggerFactory.Instance);
        var current = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
        current.Start();

        for (int i = 0; i < 200; i++)
        {
            HttpContext context = CreateRequest();

            obs.ProcessEvent("Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop", new
            {
                HttpContext = context
            });
        }

        Assert.Equal(option.CurrentValue.Capacity, obs.Queue.Count);
        var result = (HttpTracesV1)obs.GetTraces();
        Assert.Equal(option.CurrentValue.Capacity, result.Traces.Count);
        Assert.Equal(option.CurrentValue.Capacity, obs.Queue.Count);

        listener.Dispose();
    }

    private HttpContext CreateRequest()
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = "GET";
        context.Request.Path = new PathString("/myPath");
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost:1111");
        context.Request.QueryString = new QueryString("?foo=bar&bar=foo");
        context.Request.Headers.Add("Header1", new StringValues("header1Value"));
        context.Request.Headers.Add("Header2", new StringValues("header2Value"));
        return context;
    }
}
