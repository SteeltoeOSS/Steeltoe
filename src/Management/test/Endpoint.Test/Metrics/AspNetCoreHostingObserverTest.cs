// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test;

public class AspNetCoreHostingObserverTest : BaseTest
{
    // [Fact] TODO: Do we need these views
    // public void Constructor_RegistersExpectedViews()
    // {
    //    var options = new MetricsObserverOptions();
    //    var viewRegistry = new ViewRegistry();
    //    var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);
    //    Assert.Contains(viewRegistry.Views, v => v.Key == "http.server.requests.seconds");
    //    Assert.Contains(viewRegistry.Views, v => v.Key == "http.server.requests.count");
    // }
    [Fact]
    public void ShouldIgnore_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();

        var viewRegistry = new ViewRegistry();
        var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);

        Assert.True(observer.ShouldIgnoreRequest("/cloudfoundryapplication/info"));
        Assert.True(observer.ShouldIgnoreRequest("/cloudfoundryapplication/health"));
        Assert.True(observer.ShouldIgnoreRequest("/foo/bar/image.png"));
        Assert.True(observer.ShouldIgnoreRequest("/foo/bar/image.gif"));
        Assert.True(observer.ShouldIgnoreRequest("/favicon.ico"));
        Assert.True(observer.ShouldIgnoreRequest("/foo.js"));
        Assert.True(observer.ShouldIgnoreRequest("/foo.css"));
        Assert.True(observer.ShouldIgnoreRequest("/javascript/foo.js"));
        Assert.True(observer.ShouldIgnoreRequest("/css/foo.css"));
        Assert.True(observer.ShouldIgnoreRequest("/foo.html"));
        Assert.True(observer.ShouldIgnoreRequest("/html/foo.html"));
        Assert.False(observer.ShouldIgnoreRequest("/api/test"));
        Assert.False(observer.ShouldIgnoreRequest("/v2/apps"));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void ProcessEvent_IgnoresNulls()
#pragma warning restore S2699 // Tests should include assertions
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);

        observer.ProcessEvent("foobar", null);
        observer.ProcessEvent(AspNetCoreHostingObserver.StopEvent, null);

        var act = new Activity("Test");
        act.Start();
        observer.ProcessEvent(AspNetCoreHostingObserver.StopEvent, null);
        act.Stop();
    }

    [Fact]
    public void GetException_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);

        HttpContext context = GetHttpRequestMessage();
        string exception = observer.GetException(context);
        Assert.Equal("None", exception);

        context = GetHttpRequestMessage();

        var exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = new ArgumentNullException()
        };

        context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        exception = observer.GetException(context);
        Assert.Equal("ArgumentNullException", exception);
    }

    [Fact]
    public void GetLabelSets_ReturnsExpected()
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);

        HttpContext context = GetHttpRequestMessage();

        var exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = new ArgumentNullException()
        };

        context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        context.Response.StatusCode = 404;

        List<KeyValuePair<string, object>> tagContext = observer.GetLabelSets(context).ToList();

        Assert.Contains(KeyValuePair.Create("exception", (object)"ArgumentNullException"), tagContext);
        Assert.Contains(KeyValuePair.Create("uri", (object)"/foobar"), tagContext);
        Assert.Contains(KeyValuePair.Create("status", (object)"404"), tagContext);
        Assert.Contains(KeyValuePair.Create("method", (object)"GET"), tagContext);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
#pragma warning disable S2699 // Tests should include assertions
    public void HandleStopEvent_RecordsStats()
#pragma warning restore S2699 // Tests should include assertions
    {
        var options = new MetricsObserverOptions();
        var viewRegistry = new ViewRegistry();
        var observer = new AspNetCoreHostingObserver(options, viewRegistry, null);

        HttpContext context = GetHttpRequestMessage();

        var exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = new ArgumentNullException()
        };

        context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        context.Response.StatusCode = 500;

        var act = new Activity("Test");
        act.Start();
        Thread.Sleep(1000);
        act.SetEndTime(DateTime.UtcNow);

        observer.HandleStopEvent(act, context);
        observer.HandleStopEvent(act, context);

        // var requestTime = processor.GetMetricByName<double>("http.server.requests.seconds");
        // Assert.NotNull(requestTime);
        // Assert.Equal(2, requestTime.Count);
        // Assert.True(requestTime.Sum / 2 > 1);
        // Assert.True(requestTime.Max > 1);
        act.Stop();
    }

    private HttpContext GetHttpRequestMessage()
    {
        return GetHttpRequestMessage("GET", "/foobar");
    }

    private HttpContext GetHttpRequestMessage(string method, string path)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Request.Body = new MemoryStream();
        context.Response.Body = new MemoryStream();

        context.Request.Method = method;
        context.Request.Path = new PathString(path);
        context.Request.Scheme = "http";

        context.Request.Host = new HostString("localhost", 5555);
        return context;
    }

    private sealed class ExceptionHandlerFeature : IExceptionHandlerFeature
    {
        public Exception Error { get; set; }
    }
}
