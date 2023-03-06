// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class AspNetCoreHostingObserverTest : BaseTest
{
    [Fact]
    public void ShouldIgnore_ReturnsExpected()
    {
        var options = GetOptionsMonitorFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>();
        var observer = new AspNetCoreHostingObserver(options, null);

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

    [Fact]
    public void GetException_ReturnsExpected()
    {
        var options = GetOptionsMonitorFromSettings<MetricsObserverOptions,ConfigureMetricsObserverOptions>();
        var observer = new AspNetCoreHostingObserver(options, null);

        HttpContext context = GetHttpRequestMessage();
        string exception = observer.GetException(context);
        Assert.Equal("None", exception);

        context = GetHttpRequestMessage();

        var exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = new Exception()
        };

        context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        exception = observer.GetException(context);
        Assert.Equal("Exception", exception);
    }

    [Fact]
    public void GetLabelSets_ReturnsExpected()
    {
        var options = GetOptionsMonitorFromSettings<MetricsObserverOptions, ConfigureMetricsObserverOptions>();
        var observer = new AspNetCoreHostingObserver(options, null);

        HttpContext context = GetHttpRequestMessage();

        var exceptionHandlerFeature = new ExceptionHandlerFeature
        {
            Error = new Exception()
        };

        context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
        context.Response.StatusCode = 404;

        List<KeyValuePair<string, object>> tagContext = observer.GetLabelSets(context).ToList();

        Assert.Contains(KeyValuePair.Create("exception", (object)"Exception"), tagContext);
        Assert.Contains(KeyValuePair.Create("uri", (object)"/foobar"), tagContext);
        Assert.Contains(KeyValuePair.Create("status", (object)"404"), tagContext);
        Assert.Contains(KeyValuePair.Create("method", (object)"GET"), tagContext);
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
